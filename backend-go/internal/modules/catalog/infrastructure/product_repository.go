package infrastructure

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgxpool"

	"pimly.commerslab/backend-go/internal/modules/catalog/domain/products"
	"pimly.commerslab/backend-go/internal/outbox"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// ProductRepository, catalog.products + product_items + product_images
// tablolarının pgx uygulamasıdır. Şema/model ad kayması not edilmelidir:
// products.product_sku kolonu ModelCode'u, title kolonu Name'i taşır (EF
// eşlemesiyle birebir). attribute_values/variants/variant_values jsonb
// belgeleri .NET serileştiricisinin ürettiği snake_case biçimiyle uyumludur.
// Add/Update/Remove, aggregate'in bekleyen olaylarını AYNI transaction'da
// catalog.outbox_messages'a yazar (işlemsel outbox).
type ProductRepository struct {
	pool *pgxpool.Pool
}

// NewProductRepository, verilen havuzla ürün deposunu oluşturur.
func NewProductRepository(pool *pgxpool.Pool) *ProductRepository {
	return &ProductRepository{pool: pool}
}

const productColumns = `id, group_id, product_sku, title, status, attribute_values, variants,
	category_id, group_code, slicer_value, brand_id, description`

// scanProduct, tek ürün satırını okur (kalem/görseller ayrı yüklenir).
func scanProduct(row pgx.Row) (*products.Product, error) {
	var p products.Product
	var attributeValues, variantRefs []byte
	var status string
	err := row.Scan(&p.ID, &p.GroupID, &p.ModelCode, &p.Name, &status, &attributeValues, &variantRefs,
		&p.CategoryID, &p.GroupCode, &p.SlicerValue, &p.BrandID, &p.Description)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("catalog: ürün okunamadı: %w", err)
	}
	p.Status = products.Status(status)
	if err := json.Unmarshal(attributeValues, &p.AttributeValues); err != nil {
		return nil, fmt.Errorf("catalog: ürün özellik değerleri çözümlenemedi: %w", err)
	}
	if err := json.Unmarshal(variantRefs, &p.Variants); err != nil {
		return nil, fmt.Errorf("catalog: ürün eksenleri çözümlenemedi: %w", err)
	}
	return &p, nil
}

// GetByID, ürünü kalemleri ve görselleriyle döner; yoksa nil.
func (r *ProductRepository) GetByID(ctx context.Context, tenantID, id uuid.UUID) (*products.Product, error) {
	product, err := scanProduct(r.pool.QueryRow(ctx,
		`SELECT `+productColumns+` FROM catalog.products WHERE tenant_id = $1 AND id = $2`,
		tenantID, id))
	if err != nil || product == nil {
		return product, err
	}
	if err := r.loadChildren(ctx, product); err != nil {
		return nil, err
	}
	return product, nil
}

// loadChildren, ürünün kalem ve görsellerini yükler.
func (r *ProductRepository) loadChildren(ctx context.Context, p *products.Product) error {
	itemRows, err := r.pool.Query(ctx,
		`SELECT id, sku, barcode, gtin, mpn, axis_value_entry_id, axis_value, attribute_values, variant_values
		 FROM catalog.product_items WHERE product_id = $1`, p.ID)
	if err != nil {
		return fmt.Errorf("catalog: ürün kalemleri okunamadı: %w", err)
	}
	defer itemRows.Close()
	for itemRows.Next() {
		var item products.ProductItem
		var attributeValues, variantValues []byte
		if err := itemRows.Scan(&item.ID, &item.Sku, &item.Barcode, &item.Gtin, &item.Mpn,
			&item.AxisValueEntryID, &item.AxisValue, &attributeValues, &variantValues); err != nil {
			return fmt.Errorf("catalog: ürün kalemi okunamadı: %w", err)
		}
		if err := json.Unmarshal(attributeValues, &item.AttributeValues); err != nil {
			return fmt.Errorf("catalog: kalem özellik değerleri çözümlenemedi: %w", err)
		}
		if err := json.Unmarshal(variantValues, &item.VariantValues); err != nil {
			return fmt.Errorf("catalog: kalem eksen değerleri çözümlenemedi: %w", err)
		}
		p.Items = append(p.Items, &item)
	}
	if err := itemRows.Err(); err != nil {
		return err
	}

	imageRows, err := r.pool.Query(ctx,
		`SELECT id, url, sort_order, alt_text, is_primary, variant_value_id
		 FROM catalog.product_images WHERE product_id = $1`, p.ID)
	if err != nil {
		return fmt.Errorf("catalog: ürün görselleri okunamadı: %w", err)
	}
	defer imageRows.Close()
	for imageRows.Next() {
		var image products.ProductImage
		if err := imageRows.Scan(&image.ID, &image.URL, &image.SortOrder, &image.AltText,
			&image.IsPrimary, &image.VariantValueID); err != nil {
			return fmt.Errorf("catalog: ürün görseli okunamadı: %w", err)
		}
		p.Images = append(p.Images, &image)
	}
	return imageRows.Err()
}

// List, ürünleri grup + ada göre sıralı ve sayfalanmış döner (kalem/görseller dahil).
func (r *ProductRepository) List(ctx context.Context, tenantID uuid.UUID, p sharedkernel.Pagination) (sharedkernel.PagedResult[*products.Product], error) {
	var total int
	if err := r.pool.QueryRow(ctx,
		`SELECT count(*) FROM catalog.products WHERE tenant_id = $1`, tenantID).Scan(&total); err != nil {
		return sharedkernel.PagedResult[*products.Product]{}, fmt.Errorf("catalog: ürünler sayılamadı: %w", err)
	}

	rows, err := r.pool.Query(ctx,
		`SELECT `+productColumns+` FROM catalog.products
		 WHERE tenant_id = $1 ORDER BY group_id, title OFFSET $2 LIMIT $3`,
		tenantID, p.Skip(), p.PageSize)
	if err != nil {
		return sharedkernel.PagedResult[*products.Product]{}, fmt.Errorf("catalog: ürünler listelenemedi: %w", err)
	}
	defer rows.Close()

	items := []*products.Product{}
	for rows.Next() {
		product, err := scanProduct(rows)
		if err != nil {
			return sharedkernel.PagedResult[*products.Product]{}, err
		}
		items = append(items, product)
	}
	if err := rows.Err(); err != nil {
		return sharedkernel.PagedResult[*products.Product]{}, err
	}
	for _, product := range items {
		if err := r.loadChildren(ctx, product); err != nil {
			return sharedkernel.PagedResult[*products.Product]{}, err
		}
	}
	return sharedkernel.NewPagedResult(items, p, total), nil
}

// GetByItemID, kalem kimliğinin ait olduğu ürünü döner; yoksa nil.
func (r *ProductRepository) GetByItemID(ctx context.Context, tenantID, itemID uuid.UUID) (*products.Product, error) {
	var productID uuid.UUID
	err := r.pool.QueryRow(ctx,
		`SELECT product_id FROM catalog.product_items WHERE tenant_id = $1 AND id = $2`,
		tenantID, itemID).Scan(&productID)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("catalog: kalem sahibi ürün bulunamadı: %w", err)
	}
	return r.GetByID(ctx, tenantID, productID)
}

// GetByImageID, görsel kimliğinin ait olduğu ürünü döner; yoksa nil.
func (r *ProductRepository) GetByImageID(ctx context.Context, tenantID, imageID uuid.UUID) (*products.Product, error) {
	var productID uuid.UUID
	err := r.pool.QueryRow(ctx,
		`SELECT product_id FROM catalog.product_images WHERE tenant_id = $1 AND id = $2`,
		tenantID, imageID).Scan(&productID)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("catalog: görsel sahibi ürün bulunamadı: %w", err)
	}
	return r.GetByID(ctx, tenantID, productID)
}

// ModelCodeExists, model kodunun tenant genelinde kullanımda olup olmadığını döner.
func (r *ProductRepository) ModelCodeExists(ctx context.Context, tenantID uuid.UUID, modelCode string) (bool, error) {
	var exists bool
	err := r.pool.QueryRow(ctx,
		`SELECT EXISTS (SELECT 1 FROM catalog.products WHERE tenant_id = $1 AND product_sku = $2)`,
		tenantID, modelCode).Scan(&exists)
	return exists, err
}

// BarcodeExists, barkodun tenant genelinde kullanımda olup olmadığını döner.
func (r *ProductRepository) BarcodeExists(ctx context.Context, tenantID uuid.UUID, barcode string) (bool, error) {
	var exists bool
	err := r.pool.QueryRow(ctx,
		`SELECT EXISTS (SELECT 1 FROM catalog.product_items WHERE tenant_id = $1 AND barcode = $2)`,
		tenantID, barcode).Scan(&exists)
	return exists, err
}

// VariantSkuExists, kalem SKU'sunun tenant genelinde kullanımda olup olmadığını döner.
func (r *ProductRepository) VariantSkuExists(ctx context.Context, tenantID uuid.UUID, sku string) (bool, error) {
	var exists bool
	err := r.pool.QueryRow(ctx,
		`SELECT EXISTS (SELECT 1 FROM catalog.product_items WHERE tenant_id = $1 AND sku = $2)`,
		tenantID, sku).Scan(&exists)
	return exists, err
}

// AddAll, ürünleri ve bekleyen olaylarını tek transaction'da ekler.
func (r *ProductRepository) AddAll(ctx context.Context, tenantID uuid.UUID, items []*products.Product) error {
	tx, err := r.pool.Begin(ctx)
	if err != nil {
		return fmt.Errorf("catalog: ürün ekleme işlemi başlatılamadı: %w", err)
	}
	defer func() { _ = tx.Rollback(ctx) }()

	for _, product := range items {
		if err := insertProduct(ctx, tx, tenantID, product); err != nil {
			return err
		}
		if err := outbox.Write(ctx, tx, "catalog", tenantID, product.PendingEvents()); err != nil {
			return err
		}
	}
	if err := tx.Commit(ctx); err != nil {
		return err
	}
	for _, product := range items {
		product.ClearPendingEvents()
	}
	return nil
}

// insertProduct, ürünü ve alt kayıtlarını ekler.
func insertProduct(ctx context.Context, tx pgx.Tx, tenantID uuid.UUID, p *products.Product) error {
	attributeValues, variantRefs, err := marshalProductDocs(p)
	if err != nil {
		return err
	}
	if _, err := tx.Exec(ctx,
		`INSERT INTO catalog.products
		   (id, group_id, product_sku, title, status, attribute_values, variants,
		    tenant_id, category_id, group_code, slicer_value, brand_id, description)
		 VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,$12,$13)`,
		p.ID, p.GroupID, p.ModelCode, p.Name, string(p.Status), attributeValues, variantRefs,
		tenantID, p.CategoryID, p.GroupCode, p.SlicerValue, p.BrandID, p.Description); err != nil {
		return fmt.Errorf("catalog: ürün eklenemedi: %w", err)
	}
	if err := upsertProductChildren(ctx, tx, tenantID, p, false); err != nil {
		return err
	}
	return nil
}

// marshalProductDocs, ürünün jsonb belgelerini üretir.
func marshalProductDocs(p *products.Product) ([]byte, []byte, error) {
	attributeValues, err := json.Marshal(p.AttributeValues)
	if err != nil {
		return nil, nil, fmt.Errorf("catalog: özellik değerleri serileştirilemedi: %w", err)
	}
	variantRefs, err := json.Marshal(p.Variants)
	if err != nil {
		return nil, nil, fmt.Errorf("catalog: eksenler serileştirilemedi: %w", err)
	}
	return attributeValues, variantRefs, nil
}

// upsertProductChildren, kalem ve görsel koleksiyonlarını eşitler; sync=true
// iken domain'de artık bulunmayan satırlar silinir.
func upsertProductChildren(ctx context.Context, tx pgx.Tx, tenantID uuid.UUID, p *products.Product, sync bool) error {
	if sync {
		keepItems := make([]uuid.UUID, 0, len(p.Items))
		for _, item := range p.Items {
			keepItems = append(keepItems, item.ID)
		}
		if _, err := tx.Exec(ctx,
			`DELETE FROM catalog.product_items WHERE product_id = $1 AND NOT (id = ANY($2))`,
			p.ID, keepItems); err != nil {
			return fmt.Errorf("catalog: eski kalemler silinemedi: %w", err)
		}
		keepImages := make([]uuid.UUID, 0, len(p.Images))
		for _, image := range p.Images {
			keepImages = append(keepImages, image.ID)
		}
		if _, err := tx.Exec(ctx,
			`DELETE FROM catalog.product_images WHERE product_id = $1 AND NOT (id = ANY($2))`,
			p.ID, keepImages); err != nil {
			return fmt.Errorf("catalog: eski görseller silinemedi: %w", err)
		}
	}

	for _, item := range p.Items {
		attributeValues, err := json.Marshal(item.AttributeValues)
		if err != nil {
			return fmt.Errorf("catalog: kalem özellik değerleri serileştirilemedi: %w", err)
		}
		variantValues, err := json.Marshal(item.VariantValues)
		if err != nil {
			return fmt.Errorf("catalog: kalem eksen değerleri serileştirilemedi: %w", err)
		}
		if _, err := tx.Exec(ctx,
			`INSERT INTO catalog.product_items
			   (id, sku, barcode, gtin, mpn, axis_value_entry_id, axis_value,
			    attribute_values, variant_values, product_id, tenant_id)
			 VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11)
			 ON CONFLICT (id) DO UPDATE SET sku = $2, barcode = $3, gtin = $4, mpn = $5,
			   axis_value_entry_id = $6, axis_value = $7, attribute_values = $8, variant_values = $9`,
			item.ID, item.Sku, item.Barcode, item.Gtin, item.Mpn, item.AxisValueEntryID,
			item.AxisValue, attributeValues, variantValues, p.ID, tenantID); err != nil {
			return fmt.Errorf("catalog: kalem yazılamadı: %w", err)
		}
	}
	for _, image := range p.Images {
		if _, err := tx.Exec(ctx,
			`INSERT INTO catalog.product_images
			   (id, url, sort_order, alt_text, is_primary, variant_value_id, product_id, tenant_id)
			 VALUES ($1,$2,$3,$4,$5,$6,$7,$8)
			 ON CONFLICT (id) DO UPDATE SET url = $2, sort_order = $3, alt_text = $4,
			   is_primary = $5, variant_value_id = $6`,
			image.ID, image.URL, image.SortOrder, image.AltText, image.IsPrimary,
			image.VariantValueID, p.ID, tenantID); err != nil {
			return fmt.Errorf("catalog: görsel yazılamadı: %w", err)
		}
	}
	return nil
}

// Update, ürünü, alt koleksiyonlarını ve bekleyen olaylarını tek transaction'da
// kalıcılaştırır.
func (r *ProductRepository) Update(ctx context.Context, tenantID uuid.UUID, p *products.Product) error {
	tx, err := r.pool.Begin(ctx)
	if err != nil {
		return fmt.Errorf("catalog: ürün güncelleme işlemi başlatılamadı: %w", err)
	}
	defer func() { _ = tx.Rollback(ctx) }()

	attributeValues, variantRefs, err := marshalProductDocs(p)
	if err != nil {
		return err
	}
	if _, err := tx.Exec(ctx,
		`UPDATE catalog.products SET group_id = $3, product_sku = $4, title = $5, status = $6,
		   attribute_values = $7, variants = $8, category_id = $9, group_code = $10,
		   slicer_value = $11, brand_id = $12, description = $13
		 WHERE tenant_id = $1 AND id = $2`,
		tenantID, p.ID, p.GroupID, p.ModelCode, p.Name, string(p.Status),
		attributeValues, variantRefs, p.CategoryID, p.GroupCode, p.SlicerValue,
		p.BrandID, p.Description); err != nil {
		return fmt.Errorf("catalog: ürün güncellenemedi: %w", err)
	}
	if err := upsertProductChildren(ctx, tx, tenantID, p, true); err != nil {
		return err
	}
	if err := outbox.Write(ctx, tx, "catalog", tenantID, p.PendingEvents()); err != nil {
		return err
	}
	if err := tx.Commit(ctx); err != nil {
		return err
	}
	p.ClearPendingEvents()
	return nil
}

// Remove, ürünü ve bekleyen olaylarını tek transaction'da siler
// (kalem/görseller veritabanı cascade'iyle temizlenir).
func (r *ProductRepository) Remove(ctx context.Context, tenantID uuid.UUID, p *products.Product) error {
	tx, err := r.pool.Begin(ctx)
	if err != nil {
		return fmt.Errorf("catalog: ürün silme işlemi başlatılamadı: %w", err)
	}
	defer func() { _ = tx.Rollback(ctx) }()

	if _, err := tx.Exec(ctx,
		`DELETE FROM catalog.products WHERE tenant_id = $1 AND id = $2`, tenantID, p.ID); err != nil {
		return fmt.Errorf("catalog: ürün silinemedi: %w", err)
	}
	if err := outbox.Write(ctx, tx, "catalog", tenantID, p.PendingEvents()); err != nil {
		return err
	}
	if err := tx.Commit(ctx); err != nil {
		return err
	}
	p.ClearPendingEvents()
	return nil
}
