package infrastructure

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"strings"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgxpool"

	"pimly.commerslab/backend-go/internal/modules/channels/application"
)

// CatalogGateway, Channels'ın Catalog şemasından okuduğu ACL uyarlayıcısıdır
// (.NET Pimly.Integration gateway'lerinin süreç içi karşılığı; aynı veritabanında
// şemalar arası salt-okunur sorgular).
type CatalogGateway struct {
	pool *pgxpool.Pool
}

// NewCatalogGateway, verilen havuzla gateway'i oluşturur.
func NewCatalogGateway(pool *pgxpool.Pool) *CatalogGateway {
	return &CatalogGateway{pool: pool}
}

// GetCategorySnapshot, kategori özetini döner; yoksa nil.
func (g *CatalogGateway) GetCategorySnapshot(ctx context.Context, tenantID, categoryID uuid.UUID) (*application.CatalogCategorySnapshotDto, error) {
	var dto application.CatalogCategorySnapshotDto
	err := g.pool.QueryRow(ctx,
		`SELECT id, name, code FROM catalog.categories WHERE tenant_id = $1 AND id = $2`,
		tenantID, categoryID).Scan(&dto.ID, &dto.Name, &dto.Code)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("channels: catalog kategorisi okunamadı: %w", err)
	}
	return &dto, nil
}

// GetAttributeSnapshot, özellik özetini döner; yoksa nil.
func (g *CatalogGateway) GetAttributeSnapshot(ctx context.Context, tenantID, attributeID uuid.UUID) (*application.CatalogAttributeSnapshotDto, error) {
	var dto application.CatalogAttributeSnapshotDto
	err := g.pool.QueryRow(ctx,
		`SELECT id, key, name FROM catalog.attributes WHERE tenant_id = $1 AND id = $2`,
		tenantID, attributeID).Scan(&dto.ID, &dto.Key, &dto.Name)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("channels: catalog özelliği okunamadı: %w", err)
	}
	return &dto, nil
}

// AttributeBelongsToCategory, özelliğin kategoriye atanmış olup olmadığını döner.
func (g *CatalogGateway) AttributeBelongsToCategory(ctx context.Context, tenantID, categoryID, attributeID uuid.UUID) (bool, error) {
	var exists bool
	err := g.pool.QueryRow(ctx,
		`SELECT EXISTS (
		   SELECT 1 FROM catalog.category_attributes ca
		   JOIN catalog.categories c ON c.id = ca.category_id
		   WHERE c.tenant_id = $1 AND ca.category_id = $2 AND ca.attribute_id = $3)`,
		tenantID, categoryID, attributeID).Scan(&exists)
	return exists, err
}

// GetAttributeValueName, özellik değerinin adını döner; yoksa nil.
func (g *CatalogGateway) GetAttributeValueName(ctx context.Context, tenantID, attributeID, valueID uuid.UUID) (*string, error) {
	var name string
	err := g.pool.QueryRow(ctx,
		`SELECT av.name FROM catalog.attribute_values av
		 JOIN catalog.attributes a ON a.id = av.attribute_id
		 WHERE a.tenant_id = $1 AND av.attribute_id = $2 AND av.id = $3`,
		tenantID, attributeID, valueID).Scan(&name)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("channels: özellik değeri okunamadı: %w", err)
	}
	return &name, nil
}

// GetVariantSnapshot, varyant ekseni özetini döner; yoksa nil.
func (g *CatalogGateway) GetVariantSnapshot(ctx context.Context, tenantID, variantID uuid.UUID) (*application.CatalogVariantSnapshotDto, error) {
	var dto application.CatalogVariantSnapshotDto
	err := g.pool.QueryRow(ctx,
		`SELECT id, key, name FROM catalog.variants WHERE tenant_id = $1 AND id = $2`,
		tenantID, variantID).Scan(&dto.ID, &dto.Key, &dto.Name)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("channels: catalog varyantı okunamadı: %w", err)
	}
	return &dto, nil
}

// GetVariantValueLabel, varyant değerinin etiketini döner; yoksa nil.
func (g *CatalogGateway) GetVariantValueLabel(ctx context.Context, tenantID, variantID, valueID uuid.UUID) (*string, error) {
	var label string
	err := g.pool.QueryRow(ctx,
		`SELECT vv.label FROM catalog.variant_values vv
		 JOIN catalog.variants v ON v.id = vv.variant_id
		 WHERE v.tenant_id = $1 AND vv.variant_id = $2 AND vv.id = $3`,
		tenantID, variantID, valueID).Scan(&label)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("channels: varyant değeri okunamadı: %w", err)
	}
	return &label, nil
}

// gatewayAttrValue ve gatewayVariantValue, jsonb belge biçimlerinin gateway
// tarafındaki kısmi karşılıklarıdır.
type gatewayAttrValue struct {
	Attribute struct {
		ID uuid.UUID `json:"id"`
	} `json:"attribute"`
	ID   uuid.UUID `json:"id"`
	Name string    `json:"name"`
}

type gatewayVariantValue struct {
	Variant struct {
		ID uuid.UUID `json:"id"`
	} `json:"variant"`
	ID   uuid.UUID `json:"id"`
	Name string    `json:"name"`
}

// GetListingSourcesByProduct, ürünün kalem başına içerik kaynaklarını döner
// (.NET CatalogListingSourceGateway.GetByProductAsync portu): marka bilgisi,
// slicer değeri çözümü ve ürün+kalem seçimlerinin birleşimi dahil.
func (g *CatalogGateway) GetListingSourcesByProduct(ctx context.Context, tenantID, productID uuid.UUID) ([]application.CatalogListingSource, error) {
	var (
		categoryID                     uuid.UUID
		title, modelCode               string
		description, slicerValue       *string
		brandID                        *uuid.UUID
		productAttrJSON                []byte
	)
	err := g.pool.QueryRow(ctx,
		`SELECT category_id, title, description, product_sku, slicer_value, brand_id, attribute_values
		 FROM catalog.products WHERE tenant_id = $1 AND id = $2`,
		tenantID, productID).
		Scan(&categoryID, &title, &description, &modelCode, &slicerValue, &brandID, &productAttrJSON)
	if errors.Is(err, pgx.ErrNoRows) {
		return []application.CatalogListingSource{}, nil
	}
	if err != nil {
		return nil, fmt.Errorf("channels: ürün okunamadı: %w", err)
	}

	var productAttrs []gatewayAttrValue
	if err := json.Unmarshal(productAttrJSON, &productAttrs); err != nil {
		return nil, fmt.Errorf("channels: ürün özellik belgesi çözümlenemedi: %w", err)
	}

	// Marka bilgisi.
	var brandName, brandCode *string
	if brandID != nil {
		err := g.pool.QueryRow(ctx,
			`SELECT name, code FROM catalog.brands WHERE tenant_id = $1 AND id = $2`,
			tenantID, *brandID).Scan(&brandName, &brandCode)
		if err != nil && !errors.Is(err, pgx.ErrNoRows) {
			return nil, fmt.Errorf("channels: marka okunamadı: %w", err)
		}
	}

	// Slicer seçimi: bölünmüş ürünün renk değeri kalem seçimlerinde taşınmaz;
	// slicer ekseninin değer etiketinden çözülür ki eşleme/hazırlıkta görünür olsun.
	var slicerSelection *application.CatalogListingSelection
	if slicerValue != nil && strings.TrimSpace(*slicerValue) != "" {
		var variantID, valueID uuid.UUID
		var valueLabel string
		err := g.pool.QueryRow(ctx,
			`SELECT v.id, vv.id, vv.label FROM catalog.variants v
			 JOIN catalog.variant_values vv ON vv.variant_id = v.id
			 WHERE v.tenant_id = $1 AND v.slicer = TRUE AND lower(vv.label) = lower($2)
			 LIMIT 1`, tenantID, strings.TrimSpace(*slicerValue)).
			Scan(&variantID, &valueID, &valueLabel)
		if err == nil {
			slicerSelection = &application.CatalogListingSelection{
				IsVariant: true, SourceID: variantID, ValueID: valueID, ValueLabel: valueLabel}
		} else if !errors.Is(err, pgx.ErrNoRows) {
			return nil, fmt.Errorf("channels: slicer değeri çözülemedi: %w", err)
		}
	}

	// Görseller: birincil önce, sonra sıraya göre.
	imageRows, err := g.pool.Query(ctx,
		`SELECT url FROM catalog.product_images WHERE product_id = $1
		 ORDER BY is_primary DESC, sort_order`, productID)
	if err != nil {
		return nil, fmt.Errorf("channels: ürün görselleri okunamadı: %w", err)
	}
	imageURLs := []string{}
	for imageRows.Next() {
		var url string
		if err := imageRows.Scan(&url); err != nil {
			imageRows.Close()
			return nil, err
		}
		imageURLs = append(imageURLs, url)
	}
	imageRows.Close()

	// Kalemler.
	itemRows, err := g.pool.Query(ctx,
		`SELECT id, barcode, sku, attribute_values, variant_values
		 FROM catalog.product_items WHERE tenant_id = $1 AND product_id = $2`,
		tenantID, productID)
	if err != nil {
		return nil, fmt.Errorf("channels: ürün kalemleri okunamadı: %w", err)
	}
	defer itemRows.Close()

	sources := []application.CatalogListingSource{}
	for itemRows.Next() {
		var itemID uuid.UUID
		var barcode string
		var sku *string
		var itemAttrJSON, itemVarJSON []byte
		if err := itemRows.Scan(&itemID, &barcode, &sku, &itemAttrJSON, &itemVarJSON); err != nil {
			return nil, err
		}
		var itemAttrs []gatewayAttrValue
		var itemVars []gatewayVariantValue
		if err := json.Unmarshal(itemAttrJSON, &itemAttrs); err != nil {
			return nil, fmt.Errorf("channels: kalem özellik belgesi çözümlenemedi: %w", err)
		}
		if err := json.Unmarshal(itemVarJSON, &itemVars); err != nil {
			return nil, fmt.Errorf("channels: kalem eksen belgesi çözümlenemedi: %w", err)
		}

		// Ürün + kalem seçimleri birleştirilir; kalem seçimi ürününkini ezer,
		// slicer seçimi varyant seçimi olarak eklenir (.NET BuildSelections).
		type selectionKey struct {
			isVariant bool
			sourceID  uuid.UUID
		}
		byKey := map[selectionKey]application.CatalogListingSelection{}
		var order []selectionKey
		put := func(selection application.CatalogListingSelection) {
			key := selectionKey{selection.IsVariant, selection.SourceID}
			if _, exists := byKey[key]; !exists {
				order = append(order, key)
			}
			byKey[key] = selection
		}
		if slicerSelection != nil {
			put(*slicerSelection)
		}
		for _, attr := range productAttrs {
			put(application.CatalogListingSelection{
				IsVariant: false, SourceID: attr.Attribute.ID, ValueID: attr.ID, ValueLabel: attr.Name})
		}
		for _, attr := range itemAttrs {
			put(application.CatalogListingSelection{
				IsVariant: false, SourceID: attr.Attribute.ID, ValueID: attr.ID, ValueLabel: attr.Name})
		}
		for _, variant := range itemVars {
			put(application.CatalogListingSelection{
				IsVariant: true, SourceID: variant.Variant.ID, ValueID: variant.ID, ValueLabel: variant.Name})
		}
		selections := make([]application.CatalogListingSelection, len(order))
		for i, key := range order {
			selections[i] = byKey[key]
		}

		sources = append(sources, application.CatalogListingSource{
			ProductItemID: itemID, ProductID: productID, CategoryID: categoryID,
			Title: title, Description: description, BrandName: brandName,
			BrandExternalCode: brandCode, ModelCode: modelCode, Barcode: barcode, Sku: sku,
			Attributes: selections, ImageURLs: imageURLs,
		})
	}
	return sources, itemRows.Err()
}
