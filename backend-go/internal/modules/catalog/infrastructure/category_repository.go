package infrastructure

import (
	"context"
	"errors"
	"fmt"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgxpool"

	"pimly.commerslab/backend-go/internal/modules/catalog/domain/categories"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// CategoryRepository, catalog.categories + catalog.category_attributes
// tablolarının pgx uygulamasıdır. Atamalar kategori aggregate'inin parçası
// olarak yüklenir ve Update'te eşitlenir (.NET EF change-tracking karşılığı).
type CategoryRepository struct {
	pool *pgxpool.Pool
}

// NewCategoryRepository, verilen havuzla kategori deposunu oluşturur.
func NewCategoryRepository(pool *pgxpool.Pool) *CategoryRepository {
	return &CategoryRepository{pool: pool}
}

// GetByID, kategoriyi atamalarıyla birlikte döner; yoksa nil.
func (r *CategoryRepository) GetByID(ctx context.Context, tenantID, id uuid.UUID) (*categories.Category, error) {
	const query = `SELECT id, name, code, parent_id FROM catalog.categories
	               WHERE tenant_id = $1 AND id = $2`
	var c categories.Category
	err := r.pool.QueryRow(ctx, query, tenantID, id).Scan(&c.ID, &c.Name, &c.Code, &c.ParentID)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("catalog: kategori okunamadı: %w", err)
	}
	if err := r.loadAssignments(ctx, &c); err != nil {
		return nil, err
	}
	return &c, nil
}

// loadAssignments, kategorinin atamalarını yükler. category_attributes tablosu
// tenant kolonu taşımaz; tenant güvenliği sahibi kategori sorgusundan gelir.
func (r *CategoryRepository) loadAssignments(ctx context.Context, c *categories.Category) error {
	rows, err := r.pool.Query(ctx,
		`SELECT id, attribute_id, required, sort_order, scope
		 FROM catalog.category_attributes WHERE category_id = $1`, c.ID)
	if err != nil {
		return fmt.Errorf("catalog: kategori atamaları okunamadı: %w", err)
	}
	defer rows.Close()

	for rows.Next() {
		var a categories.Assignment
		var scope string
		if err := rows.Scan(&a.ID, &a.AttributeID, &a.Required, &a.SortOrder, &scope); err != nil {
			return fmt.Errorf("catalog: atama okunamadı: %w", err)
		}
		a.Scope = categories.AttributeScope(scope)
		c.Assignments = append(c.Assignments, &a)
	}
	return rows.Err()
}

// List, kategorileri ada göre sıralı ve sayfalanmış döner (atamalar liste
// görünümünde yüklenmez — .NET ListAsync ile aynı).
func (r *CategoryRepository) List(ctx context.Context, tenantID uuid.UUID, p sharedkernel.Pagination) (sharedkernel.PagedResult[*categories.Category], error) {
	var total int
	if err := r.pool.QueryRow(ctx,
		`SELECT count(*) FROM catalog.categories WHERE tenant_id = $1`, tenantID).Scan(&total); err != nil {
		return sharedkernel.PagedResult[*categories.Category]{}, fmt.Errorf("catalog: kategoriler sayılamadı: %w", err)
	}

	rows, err := r.pool.Query(ctx,
		`SELECT id, name, code, parent_id FROM catalog.categories
		 WHERE tenant_id = $1 ORDER BY name OFFSET $2 LIMIT $3`,
		tenantID, p.Skip(), p.PageSize)
	if err != nil {
		return sharedkernel.PagedResult[*categories.Category]{}, fmt.Errorf("catalog: kategoriler listelenemedi: %w", err)
	}
	defer rows.Close()

	items := []*categories.Category{}
	for rows.Next() {
		var c categories.Category
		if err := rows.Scan(&c.ID, &c.Name, &c.Code, &c.ParentID); err != nil {
			return sharedkernel.PagedResult[*categories.Category]{}, fmt.Errorf("catalog: kategori okunamadı: %w", err)
		}
		items = append(items, &c)
	}
	if err := rows.Err(); err != nil {
		return sharedkernel.PagedResult[*categories.Category]{}, err
	}
	return sharedkernel.NewPagedResult(items, p, total), nil
}

// GetDescendantIDs, kategorinin tüm alt soy kimliklerini özyinelemeli CTE ile
// döner (.NET tarafı bunu bellek içinde hesaplar; sonuç aynıdır).
func (r *CategoryRepository) GetDescendantIDs(ctx context.Context, tenantID, categoryID uuid.UUID) (map[uuid.UUID]struct{}, error) {
	rows, err := r.pool.Query(ctx, `
		WITH RECURSIVE descendants AS (
			SELECT id FROM catalog.categories WHERE tenant_id = $1 AND parent_id = $2
			UNION ALL
			SELECT c.id FROM catalog.categories c
			JOIN descendants d ON c.parent_id = d.id
			WHERE c.tenant_id = $1
		)
		SELECT id FROM descendants`, tenantID, categoryID)
	if err != nil {
		return nil, fmt.Errorf("catalog: alt soy sorgulanamadı: %w", err)
	}
	defer rows.Close()

	out := map[uuid.UUID]struct{}{}
	for rows.Next() {
		var id uuid.UUID
		if err := rows.Scan(&id); err != nil {
			return nil, err
		}
		out[id] = struct{}{}
	}
	return out, rows.Err()
}

// FindByAssignmentID, atama kimliğinin ait olduğu kategoriyi döner; yoksa nil.
func (r *CategoryRepository) FindByAssignmentID(ctx context.Context, tenantID, assignmentID uuid.UUID) (*categories.Category, error) {
	const query = `SELECT ca.category_id FROM catalog.category_attributes ca
	               JOIN catalog.categories c ON c.id = ca.category_id
	               WHERE c.tenant_id = $1 AND ca.id = $2`
	var categoryID uuid.UUID
	err := r.pool.QueryRow(ctx, query, tenantID, assignmentID).Scan(&categoryID)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("catalog: atama sahibi bulunamadı: %w", err)
	}
	return r.GetByID(ctx, tenantID, categoryID)
}

// Add, yeni kategoriyi (varsa atamalarıyla) tek işlemde ekler.
func (r *CategoryRepository) Add(ctx context.Context, tenantID uuid.UUID, c *categories.Category) error {
	tx, err := r.pool.Begin(ctx)
	if err != nil {
		return fmt.Errorf("catalog: kategori ekleme işlemi başlatılamadı: %w", err)
	}
	defer func() { _ = tx.Rollback(ctx) }()

	if _, err := tx.Exec(ctx,
		`INSERT INTO catalog.categories (id, name, code, parent_id, tenant_id)
		 VALUES ($1, $2, $3, $4, $5)`,
		c.ID, c.Name, c.Code, c.ParentID, tenantID); err != nil {
		return fmt.Errorf("catalog: kategori eklenemedi: %w", err)
	}
	if err := upsertAssignments(ctx, tx, c); err != nil {
		return err
	}
	return tx.Commit(ctx)
}

// Update, kategori alanlarını ve atama koleksiyonunu tek işlemde eşitler:
// domain'de artık bulunmayan atamalar silinir, kalanlar upsert edilir.
func (r *CategoryRepository) Update(ctx context.Context, tenantID uuid.UUID, c *categories.Category) error {
	tx, err := r.pool.Begin(ctx)
	if err != nil {
		return fmt.Errorf("catalog: kategori güncelleme işlemi başlatılamadı: %w", err)
	}
	defer func() { _ = tx.Rollback(ctx) }()

	if _, err := tx.Exec(ctx,
		`UPDATE catalog.categories SET name = $3, code = $4, parent_id = $5
		 WHERE tenant_id = $1 AND id = $2`,
		tenantID, c.ID, c.Name, c.Code, c.ParentID); err != nil {
		return fmt.Errorf("catalog: kategori güncellenemedi: %w", err)
	}

	keep := make([]uuid.UUID, 0, len(c.Assignments))
	for _, a := range c.Assignments {
		keep = append(keep, a.ID)
	}
	if _, err := tx.Exec(ctx,
		`DELETE FROM catalog.category_attributes
		 WHERE category_id = $1 AND NOT (id = ANY($2))`, c.ID, keep); err != nil {
		return fmt.Errorf("catalog: eski atamalar silinemedi: %w", err)
	}
	if err := upsertAssignments(ctx, tx, c); err != nil {
		return err
	}
	return tx.Commit(ctx)
}

// upsertAssignments, kategorinin atamalarını ekler/günceller.
func upsertAssignments(ctx context.Context, tx pgx.Tx, c *categories.Category) error {
	for _, a := range c.Assignments {
		if _, err := tx.Exec(ctx,
			`INSERT INTO catalog.category_attributes (id, attribute_id, required, sort_order, category_id, scope)
			 VALUES ($1, $2, $3, $4, $5, $6)
			 ON CONFLICT (id) DO UPDATE SET required = $3, sort_order = $4, scope = $6`,
			a.ID, a.AttributeID, a.Required, a.SortOrder, c.ID, string(a.Scope)); err != nil {
			return fmt.Errorf("catalog: atama yazılamadı: %w", err)
		}
	}
	return nil
}

// Remove, kategoriyi siler; atamalar veritabanı cascade'iyle, alt kategoriler
// parent_id=NULL kuralıyla temizlenir (.NET/EF ile aynı şema davranışı).
func (r *CategoryRepository) Remove(ctx context.Context, tenantID, id uuid.UUID) error {
	_, err := r.pool.Exec(ctx,
		`DELETE FROM catalog.categories WHERE tenant_id = $1 AND id = $2`, tenantID, id)
	if err != nil {
		return fmt.Errorf("catalog: kategori silinemedi: %w", err)
	}
	return nil
}
