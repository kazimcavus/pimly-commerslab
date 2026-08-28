package infrastructure

import (
	"context"
	"errors"
	"fmt"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgxpool"

	"pimly.commerslab/backend-go/internal/modules/catalog/domain/attributes"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// AttributeRepository, catalog.attributes + catalog.attribute_values
// tablolarının pgx uygulamasıdır. Değerler özellik aggregate'inin parçası
// olarak yüklenir ve Update'te eşitlenir.
type AttributeRepository struct {
	pool *pgxpool.Pool
}

// NewAttributeRepository, verilen havuzla özellik deposunu oluşturur.
func NewAttributeRepository(pool *pgxpool.Pool) *AttributeRepository {
	return &AttributeRepository{pool: pool}
}

// GetByID, özelliği değerleriyle birlikte döner; yoksa nil.
func (r *AttributeRepository) GetByID(ctx context.Context, tenantID, id uuid.UUID) (*attributes.Attribute, error) {
	const query = `SELECT id, key, name FROM catalog.attributes
	               WHERE tenant_id = $1 AND id = $2`
	return r.loadOne(ctx, r.pool.QueryRow(ctx, query, tenantID, id))
}

// GetByKey, özelliği anahtarına göre döner; yoksa nil.
func (r *AttributeRepository) GetByKey(ctx context.Context, tenantID uuid.UUID, key string) (*attributes.Attribute, error) {
	const query = `SELECT id, key, name FROM catalog.attributes
	               WHERE tenant_id = $1 AND key = $2`
	return r.loadOne(ctx, r.pool.QueryRow(ctx, query, tenantID, key))
}

// loadOne, tek özellik satırını okuyup değerlerini yükler; satır yoksa (nil, nil).
func (r *AttributeRepository) loadOne(ctx context.Context, row pgx.Row) (*attributes.Attribute, error) {
	var a attributes.Attribute
	err := row.Scan(&a.ID, &a.Key, &a.Name)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("catalog: özellik okunamadı: %w", err)
	}
	if err := r.loadValues(ctx, &a); err != nil {
		return nil, err
	}
	return &a, nil
}

// loadValues, özelliğin değerlerini yükler. Sıralama verilmez: .NET/EF de
// vermez, Postgres ekleme sırasına yakın (heap) sırayla döner — parite için
// bu davranış korunur. attribute_values tablosu tenant kolonu taşımaz;
// güvenlik sahibi özellik sorgusundan gelir.
func (r *AttributeRepository) loadValues(ctx context.Context, a *attributes.Attribute) error {
	rows, err := r.pool.Query(ctx,
		`SELECT id, name FROM catalog.attribute_values WHERE attribute_id = $1`, a.ID)
	if err != nil {
		return fmt.Errorf("catalog: özellik değerleri okunamadı: %w", err)
	}
	defer rows.Close()

	for rows.Next() {
		var v attributes.Value
		if err := rows.Scan(&v.ID, &v.Name); err != nil {
			return fmt.Errorf("catalog: özellik değeri okunamadı: %w", err)
		}
		a.Values = append(a.Values, &v)
	}
	return rows.Err()
}

// List, özellikleri anahtara göre sıralı ve sayfalanmış döner (değerler liste
// görünümünde yüklenmez — .NET ListAsync ile aynı).
func (r *AttributeRepository) List(ctx context.Context, tenantID uuid.UUID, p sharedkernel.Pagination) (sharedkernel.PagedResult[*attributes.Attribute], error) {
	var total int
	if err := r.pool.QueryRow(ctx,
		`SELECT count(*) FROM catalog.attributes WHERE tenant_id = $1`, tenantID).Scan(&total); err != nil {
		return sharedkernel.PagedResult[*attributes.Attribute]{}, fmt.Errorf("catalog: özellikler sayılamadı: %w", err)
	}

	rows, err := r.pool.Query(ctx,
		`SELECT id, key, name FROM catalog.attributes
		 WHERE tenant_id = $1 ORDER BY key OFFSET $2 LIMIT $3`,
		tenantID, p.Skip(), p.PageSize)
	if err != nil {
		return sharedkernel.PagedResult[*attributes.Attribute]{}, fmt.Errorf("catalog: özellikler listelenemedi: %w", err)
	}
	defer rows.Close()

	items := []*attributes.Attribute{}
	for rows.Next() {
		var a attributes.Attribute
		if err := rows.Scan(&a.ID, &a.Key, &a.Name); err != nil {
			return sharedkernel.PagedResult[*attributes.Attribute]{}, fmt.Errorf("catalog: özellik okunamadı: %w", err)
		}
		items = append(items, &a)
	}
	if err := rows.Err(); err != nil {
		return sharedkernel.PagedResult[*attributes.Attribute]{}, err
	}
	return sharedkernel.NewPagedResult(items, p, total), nil
}

// FindByValueID, değer kimliğinin ait olduğu özelliği döner; yoksa nil.
func (r *AttributeRepository) FindByValueID(ctx context.Context, tenantID, valueID uuid.UUID) (*attributes.Attribute, error) {
	const query = `SELECT av.attribute_id FROM catalog.attribute_values av
	               JOIN catalog.attributes a ON a.id = av.attribute_id
	               WHERE a.tenant_id = $1 AND av.id = $2`
	var attributeID uuid.UUID
	err := r.pool.QueryRow(ctx, query, tenantID, valueID).Scan(&attributeID)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("catalog: değer sahibi bulunamadı: %w", err)
	}
	return r.GetByID(ctx, tenantID, attributeID)
}

// Add, yeni özelliği (varsa değerleriyle) tek işlemde ekler.
func (r *AttributeRepository) Add(ctx context.Context, tenantID uuid.UUID, a *attributes.Attribute) error {
	tx, err := r.pool.Begin(ctx)
	if err != nil {
		return fmt.Errorf("catalog: özellik ekleme işlemi başlatılamadı: %w", err)
	}
	defer func() { _ = tx.Rollback(ctx) }()

	if _, err := tx.Exec(ctx,
		`INSERT INTO catalog.attributes (id, key, name, tenant_id) VALUES ($1, $2, $3, $4)`,
		a.ID, a.Key, a.Name, tenantID); err != nil {
		return fmt.Errorf("catalog: özellik eklenemedi: %w", err)
	}
	if err := upsertValues(ctx, tx, a); err != nil {
		return err
	}
	return tx.Commit(ctx)
}

// Update, özellik alanlarını ve değer koleksiyonunu tek işlemde eşitler.
func (r *AttributeRepository) Update(ctx context.Context, tenantID uuid.UUID, a *attributes.Attribute) error {
	tx, err := r.pool.Begin(ctx)
	if err != nil {
		return fmt.Errorf("catalog: özellik güncelleme işlemi başlatılamadı: %w", err)
	}
	defer func() { _ = tx.Rollback(ctx) }()

	if _, err := tx.Exec(ctx,
		`UPDATE catalog.attributes SET name = $3 WHERE tenant_id = $1 AND id = $2`,
		tenantID, a.ID, a.Name); err != nil {
		return fmt.Errorf("catalog: özellik güncellenemedi: %w", err)
	}

	keep := make([]uuid.UUID, 0, len(a.Values))
	for _, v := range a.Values {
		keep = append(keep, v.ID)
	}
	if _, err := tx.Exec(ctx,
		`DELETE FROM catalog.attribute_values
		 WHERE attribute_id = $1 AND NOT (id = ANY($2))`, a.ID, keep); err != nil {
		return fmt.Errorf("catalog: eski değerler silinemedi: %w", err)
	}
	if err := upsertValues(ctx, tx, a); err != nil {
		return err
	}
	return tx.Commit(ctx)
}

// upsertValues, özelliğin değerlerini ekler/günceller.
func upsertValues(ctx context.Context, tx pgx.Tx, a *attributes.Attribute) error {
	for _, v := range a.Values {
		if _, err := tx.Exec(ctx,
			`INSERT INTO catalog.attribute_values (id, name, attribute_id)
			 VALUES ($1, $2, $3)
			 ON CONFLICT (id) DO UPDATE SET name = $2`,
			v.ID, v.Name, a.ID); err != nil {
			return fmt.Errorf("catalog: özellik değeri yazılamadı: %w", err)
		}
	}
	return nil
}

// Remove, özelliği siler (değerler veritabanı cascade'iyle temizlenir).
func (r *AttributeRepository) Remove(ctx context.Context, tenantID, id uuid.UUID) error {
	_, err := r.pool.Exec(ctx,
		`DELETE FROM catalog.attributes WHERE tenant_id = $1 AND id = $2`, tenantID, id)
	if err != nil {
		return fmt.Errorf("catalog: özellik silinemedi: %w", err)
	}
	return nil
}
