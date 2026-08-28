package infrastructure

import (
	"context"
	"errors"
	"fmt"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgxpool"

	"pimly.commerslab/backend-go/internal/modules/catalog/domain/variants"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// VariantRepository, catalog.variants + catalog.variant_values tablolarının
// pgx uygulamasıdır. Değerler tür aggregate'inin parçası olarak yüklenir ve
// Update'te eşitlenir.
type VariantRepository struct {
	pool *pgxpool.Pool
}

// NewVariantRepository, verilen havuzla varyant deposunu oluşturur.
func NewVariantRepository(pool *pgxpool.Pool) *VariantRepository {
	return &VariantRepository{pool: pool}
}

// GetByID, türü değerleriyle birlikte döner; yoksa nil.
func (r *VariantRepository) GetByID(ctx context.Context, tenantID, id uuid.UUID) (*variants.Variant, error) {
	const query = `SELECT id, key, name, selection_style, sort_order, slicer
	               FROM catalog.variants WHERE tenant_id = $1 AND id = $2`
	return r.loadOne(ctx, r.pool.QueryRow(ctx, query, tenantID, id), true)
}

// GetByName, türü ada göre birebir eşleşmeyle döner; yoksa nil
// (.NET GetByNameAsync v.Name == name ile aynı — büyük/küçük harf duyarlı).
func (r *VariantRepository) GetByName(ctx context.Context, tenantID uuid.UUID, name string) (*variants.Variant, error) {
	const query = `SELECT id, key, name, selection_style, sort_order, slicer
	               FROM catalog.variants WHERE tenant_id = $1 AND name = $2`
	return r.loadOne(ctx, r.pool.QueryRow(ctx, query, tenantID, name), false)
}

// GetByKey, türü anahtarına göre döner; yoksa nil.
func (r *VariantRepository) GetByKey(ctx context.Context, tenantID uuid.UUID, key string) (*variants.Variant, error) {
	const query = `SELECT id, key, name, selection_style, sort_order, slicer
	               FROM catalog.variants WHERE tenant_id = $1 AND key = $2`
	return r.loadOne(ctx, r.pool.QueryRow(ctx, query, tenantID, key), false)
}

// GetSlicerVariant, slicer işaretli türü döner (excludeID hariç); yoksa nil.
func (r *VariantRepository) GetSlicerVariant(ctx context.Context, tenantID uuid.UUID, excludeID *uuid.UUID) (*variants.Variant, error) {
	query := `SELECT id, key, name, selection_style, sort_order, slicer
	          FROM catalog.variants WHERE tenant_id = $1 AND slicer = TRUE`
	args := []any{tenantID}
	if excludeID != nil {
		query += ` AND id <> $2`
		args = append(args, *excludeID)
	}
	query += ` LIMIT 1`
	return r.loadOne(ctx, r.pool.QueryRow(ctx, query, args...), false)
}

// loadOne, tek tür satırını okur; withValues ile değerleri de yükler.
func (r *VariantRepository) loadOne(ctx context.Context, row pgx.Row, withValues bool) (*variants.Variant, error) {
	var v variants.Variant
	var style string
	err := row.Scan(&v.ID, &v.Key, &v.Name, &style, &v.SortOrder, &v.Slicer)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("catalog: varyant türü okunamadı: %w", err)
	}
	v.SelectionStyle = variants.SelectionStyle(style)
	if withValues {
		if err := r.loadValues(ctx, &v); err != nil {
			return nil, err
		}
	}
	return &v, nil
}

// loadValues, türün değerlerini yükler. Sıralama verilmez (.NET/EF paritesi);
// tablo tenant kolonu taşımaz, güvenlik sahibi tür sorgusundan gelir.
func (r *VariantRepository) loadValues(ctx context.Context, v *variants.Variant) error {
	rows, err := r.pool.Query(ctx,
		`SELECT id, key, label, color, image_url, sort_order
		 FROM catalog.variant_values WHERE variant_id = $1`, v.ID)
	if err != nil {
		return fmt.Errorf("catalog: varyant değerleri okunamadı: %w", err)
	}
	defer rows.Close()

	for rows.Next() {
		var value variants.Value
		if err := rows.Scan(&value.ID, &value.Key, &value.Label, &value.Color, &value.ImageURL, &value.SortOrder); err != nil {
			return fmt.Errorf("catalog: varyant değeri okunamadı: %w", err)
		}
		v.Values = append(v.Values, &value)
	}
	return rows.Err()
}

// List, türleri sıra + ada göre sıralı ve sayfalanmış döner (değerler liste
// görünümünde yüklenmez).
func (r *VariantRepository) List(ctx context.Context, tenantID uuid.UUID, p sharedkernel.Pagination) (sharedkernel.PagedResult[*variants.Variant], error) {
	var total int
	if err := r.pool.QueryRow(ctx,
		`SELECT count(*) FROM catalog.variants WHERE tenant_id = $1`, tenantID).Scan(&total); err != nil {
		return sharedkernel.PagedResult[*variants.Variant]{}, fmt.Errorf("catalog: varyant türleri sayılamadı: %w", err)
	}

	rows, err := r.pool.Query(ctx,
		`SELECT id, key, name, selection_style, sort_order, slicer
		 FROM catalog.variants WHERE tenant_id = $1
		 ORDER BY sort_order, name OFFSET $2 LIMIT $3`,
		tenantID, p.Skip(), p.PageSize)
	if err != nil {
		return sharedkernel.PagedResult[*variants.Variant]{}, fmt.Errorf("catalog: varyant türleri listelenemedi: %w", err)
	}
	defer rows.Close()

	items := []*variants.Variant{}
	for rows.Next() {
		var v variants.Variant
		var style string
		if err := rows.Scan(&v.ID, &v.Key, &v.Name, &style, &v.SortOrder, &v.Slicer); err != nil {
			return sharedkernel.PagedResult[*variants.Variant]{}, fmt.Errorf("catalog: varyant türü okunamadı: %w", err)
		}
		v.SelectionStyle = variants.SelectionStyle(style)
		items = append(items, &v)
	}
	if err := rows.Err(); err != nil {
		return sharedkernel.PagedResult[*variants.Variant]{}, err
	}
	return sharedkernel.NewPagedResult(items, p, total), nil
}

// FindByValueID, değer kimliğinin ait olduğu türü döner; yoksa nil.
func (r *VariantRepository) FindByValueID(ctx context.Context, tenantID, valueID uuid.UUID) (*variants.Variant, error) {
	const query = `SELECT vv.variant_id FROM catalog.variant_values vv
	               JOIN catalog.variants v ON v.id = vv.variant_id
	               WHERE v.tenant_id = $1 AND vv.id = $2`
	var variantID uuid.UUID
	err := r.pool.QueryRow(ctx, query, tenantID, valueID).Scan(&variantID)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("catalog: değer sahibi tür bulunamadı: %w", err)
	}
	return r.GetByID(ctx, tenantID, variantID)
}

// Add, yeni türü (varsa değerleriyle) tek işlemde ekler.
func (r *VariantRepository) Add(ctx context.Context, tenantID uuid.UUID, v *variants.Variant) error {
	tx, err := r.pool.Begin(ctx)
	if err != nil {
		return fmt.Errorf("catalog: varyant ekleme işlemi başlatılamadı: %w", err)
	}
	defer func() { _ = tx.Rollback(ctx) }()

	if _, err := tx.Exec(ctx,
		`INSERT INTO catalog.variants (id, key, name, selection_style, sort_order, slicer, tenant_id)
		 VALUES ($1, $2, $3, $4, $5, $6, $7)`,
		v.ID, v.Key, v.Name, string(v.SelectionStyle), v.SortOrder, v.Slicer, tenantID); err != nil {
		return fmt.Errorf("catalog: varyant türü eklenemedi: %w", err)
	}
	if err := upsertVariantValues(ctx, tx, v); err != nil {
		return err
	}
	return tx.Commit(ctx)
}

// Update, tür alanlarını ve değer koleksiyonunu tek işlemde eşitler.
func (r *VariantRepository) Update(ctx context.Context, tenantID uuid.UUID, v *variants.Variant) error {
	tx, err := r.pool.Begin(ctx)
	if err != nil {
		return fmt.Errorf("catalog: varyant güncelleme işlemi başlatılamadı: %w", err)
	}
	defer func() { _ = tx.Rollback(ctx) }()

	if _, err := tx.Exec(ctx,
		`UPDATE catalog.variants SET name = $3, selection_style = $4, sort_order = $5, slicer = $6
		 WHERE tenant_id = $1 AND id = $2`,
		tenantID, v.ID, v.Name, string(v.SelectionStyle), v.SortOrder, v.Slicer); err != nil {
		return fmt.Errorf("catalog: varyant türü güncellenemedi: %w", err)
	}

	keep := make([]uuid.UUID, 0, len(v.Values))
	for _, value := range v.Values {
		keep = append(keep, value.ID)
	}
	if _, err := tx.Exec(ctx,
		`DELETE FROM catalog.variant_values
		 WHERE variant_id = $1 AND NOT (id = ANY($2))`, v.ID, keep); err != nil {
		return fmt.Errorf("catalog: eski varyant değerleri silinemedi: %w", err)
	}
	if err := upsertVariantValues(ctx, tx, v); err != nil {
		return err
	}
	return tx.Commit(ctx)
}

// upsertVariantValues, türün değerlerini ekler/günceller.
func upsertVariantValues(ctx context.Context, tx pgx.Tx, v *variants.Variant) error {
	for _, value := range v.Values {
		if _, err := tx.Exec(ctx,
			`INSERT INTO catalog.variant_values (id, key, label, color, image_url, sort_order, variant_id)
			 VALUES ($1, $2, $3, $4, $5, $6, $7)
			 ON CONFLICT (id) DO UPDATE SET key = $2, label = $3, color = $4, image_url = $5, sort_order = $6`,
			value.ID, value.Key, value.Label, value.Color, value.ImageURL, value.SortOrder, v.ID); err != nil {
			return fmt.Errorf("catalog: varyant değeri yazılamadı: %w", err)
		}
	}
	return nil
}

// Remove, türü siler (değerler veritabanı cascade'iyle temizlenir).
func (r *VariantRepository) Remove(ctx context.Context, tenantID, id uuid.UUID) error {
	_, err := r.pool.Exec(ctx,
		`DELETE FROM catalog.variants WHERE tenant_id = $1 AND id = $2`, tenantID, id)
	if err != nil {
		return fmt.Errorf("catalog: varyant türü silinemedi: %w", err)
	}
	return nil
}
