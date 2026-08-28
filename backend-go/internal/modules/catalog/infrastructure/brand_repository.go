// Package infrastructure, Catalog modülünün pgx tabanlı kalıcılık katmanını
// içerir (.NET Catalog.Infrastructure karşılığı). Kurallar:
//
//   - Her sorgu tenant_id koşulunu AÇIKÇA taşır (EF'in görünmez query filter'ı
//     yerine); CI betiği bunu mekanik olarak denetler.
//   - Kolon adları mevcut EF şemasıyla birebir aynıdır (snake_case).
//   - Sıralamalar EF sorgularının ürettiği ORDER BY'ları aynalar.
package infrastructure

import (
	"context"
	"errors"
	"fmt"
	"strings"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgxpool"

	"pimly.commerslab/backend-go/internal/modules/catalog/domain/brands"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// BrandRepository, catalog.brands tablosunun pgx uygulamasıdır.
type BrandRepository struct {
	pool *pgxpool.Pool
}

// NewBrandRepository, verilen havuzla marka deposunu oluşturur.
func NewBrandRepository(pool *pgxpool.Pool) *BrandRepository {
	return &BrandRepository{pool: pool}
}

// GetByID, kimlikle markayı döner; yoksa nil.
func (r *BrandRepository) GetByID(ctx context.Context, tenantID, id uuid.UUID) (*brands.Brand, error) {
	const query = `SELECT id, name, code FROM catalog.brands
	               WHERE tenant_id = $1 AND id = $2`
	return scanBrand(r.pool.QueryRow(ctx, query, tenantID, id))
}

// GetByName, markayı ada göre büyük/küçük harf duyarsız döner; yoksa nil.
// .NET EF.Functions.ILike davranışını aynalar (ad kırpılır, joker karakter
// kaçışı yapılmaz — mevcut davranışla bire bir).
func (r *BrandRepository) GetByName(ctx context.Context, tenantID uuid.UUID, name string) (*brands.Brand, error) {
	const query = `SELECT id, name, code FROM catalog.brands
	               WHERE tenant_id = $1 AND name ILIKE $2 LIMIT 1`
	return scanBrand(r.pool.QueryRow(ctx, query, tenantID, strings.TrimSpace(name)))
}

// List, markaları ada göre sıralı ve sayfalanmış döner.
func (r *BrandRepository) List(ctx context.Context, tenantID uuid.UUID, p sharedkernel.Pagination) (sharedkernel.PagedResult[*brands.Brand], error) {
	var total int
	if err := r.pool.QueryRow(ctx,
		`SELECT count(*) FROM catalog.brands WHERE tenant_id = $1`, tenantID).Scan(&total); err != nil {
		return sharedkernel.PagedResult[*brands.Brand]{}, fmt.Errorf("catalog: markalar sayılamadı: %w", err)
	}

	rows, err := r.pool.Query(ctx,
		`SELECT id, name, code FROM catalog.brands
		 WHERE tenant_id = $1 ORDER BY name OFFSET $2 LIMIT $3`,
		tenantID, p.Skip(), p.PageSize)
	if err != nil {
		return sharedkernel.PagedResult[*brands.Brand]{}, fmt.Errorf("catalog: markalar listelenemedi: %w", err)
	}
	defer rows.Close()

	items := []*brands.Brand{}
	for rows.Next() {
		var b brands.Brand
		if err := rows.Scan(&b.ID, &b.Name, &b.Code); err != nil {
			return sharedkernel.PagedResult[*brands.Brand]{}, fmt.Errorf("catalog: marka okunamadı: %w", err)
		}
		items = append(items, &b)
	}
	if err := rows.Err(); err != nil {
		return sharedkernel.PagedResult[*brands.Brand]{}, err
	}
	return sharedkernel.NewPagedResult(items, p, total), nil
}

// Add, yeni markayı ekler; tenant kimliği satıra açıkça damgalanır.
func (r *BrandRepository) Add(ctx context.Context, tenantID uuid.UUID, brand *brands.Brand) error {
	_, err := r.pool.Exec(ctx,
		`INSERT INTO catalog.brands (id, name, code, tenant_id) VALUES ($1, $2, $3, $4)`,
		brand.ID, brand.Name, brand.Code, tenantID)
	if err != nil {
		return fmt.Errorf("catalog: marka eklenemedi: %w", err)
	}
	return nil
}

// Update, marka değişikliklerini kalıcılaştırır.
func (r *BrandRepository) Update(ctx context.Context, tenantID uuid.UUID, brand *brands.Brand) error {
	_, err := r.pool.Exec(ctx,
		`UPDATE catalog.brands SET name = $3, code = $4
		 WHERE tenant_id = $1 AND id = $2`,
		tenantID, brand.ID, brand.Name, brand.Code)
	if err != nil {
		return fmt.Errorf("catalog: marka güncellenemedi: %w", err)
	}
	return nil
}

// Remove, markayı siler.
func (r *BrandRepository) Remove(ctx context.Context, tenantID, id uuid.UUID) error {
	_, err := r.pool.Exec(ctx,
		`DELETE FROM catalog.brands WHERE tenant_id = $1 AND id = $2`, tenantID, id)
	if err != nil {
		return fmt.Errorf("catalog: marka silinemedi: %w", err)
	}
	return nil
}

// scanBrand, tek marka satırını okur; satır yoksa (nil, nil) döner.
func scanBrand(row pgx.Row) (*brands.Brand, error) {
	var b brands.Brand
	err := row.Scan(&b.ID, &b.Name, &b.Code)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("catalog: marka okunamadı: %w", err)
	}
	return &b, nil
}
