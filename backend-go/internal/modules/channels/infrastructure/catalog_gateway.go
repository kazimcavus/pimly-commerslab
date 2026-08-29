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
		categoryID               uuid.UUID
		title, modelCode         string
		description, slicerValue *string
		brandID                  *uuid.UUID
		productAttrJSON          []byte
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

// GetListingSourcesByItems, verilen kalem kimliklerinin pazaryerine giden
// içerik kaynaklarını döner (.NET ICatalogListingSourceGateway.GetAsync
// portu; ürünler arası kalemler tek toplu sorguyla çözülür — listing-sync
// worker'ının içerik senkronunda kullanılır). Bulunmayan kalemler sonuçta yer
// almaz.
// ListItemIDsByCategories, verilen catalog kategorilerindeki tüm satılabilir
// kalem kimliklerini döner (.NET ListItemIdsByCategoriesAsync portu; yayın
// worker'ının kapsam keşfinde kullanılır).
func (g *CatalogGateway) ListItemIDsByCategories(ctx context.Context, tenantID uuid.UUID, categoryIDs []uuid.UUID) ([]uuid.UUID, error) {
	if len(categoryIDs) == 0 {
		return []uuid.UUID{}, nil
	}
	rows, err := g.pool.Query(ctx,
		`SELECT pi.id FROM catalog.product_items pi
		 JOIN catalog.products p ON p.id = pi.product_id
		 WHERE pi.tenant_id = $1 AND p.category_id = ANY($2)`, tenantID, categoryIDs)
	if err != nil {
		return nil, fmt.Errorf("channels: kategori kalemleri okunamadı: %w", err)
	}
	defer rows.Close()

	ids := []uuid.UUID{}
	for rows.Next() {
		var id uuid.UUID
		if err := rows.Scan(&id); err != nil {
			return nil, err
		}
		ids = append(ids, id)
	}
	return ids, rows.Err()
}

func (g *CatalogGateway) GetListingSourcesByItems(ctx context.Context, tenantID uuid.UUID, itemIDs []uuid.UUID) ([]application.CatalogListingSource, error) {
	if len(itemIDs) == 0 {
		return []application.CatalogListingSource{}, nil
	}

	rows, err := g.pool.Query(ctx,
		`SELECT pi.id, pi.barcode, pi.sku, pi.attribute_values, pi.variant_values,
		        p.id, p.category_id, p.title, p.description, p.product_sku,
		        p.slicer_value, p.brand_id, p.attribute_values
		 FROM catalog.product_items pi
		 JOIN catalog.products p ON p.id = pi.product_id
		 WHERE pi.tenant_id = $1 AND pi.id = ANY($2)`, tenantID, itemIDs)
	if err != nil {
		return nil, fmt.Errorf("channels: kalem içerik kaynakları okunamadı: %w", err)
	}

	type rawItem struct {
		itemID                    uuid.UUID
		barcode                   string
		sku                       *string
		itemAttrJSON, itemVarJSON []byte
		productID, categoryID     uuid.UUID
		title, modelCode          string
		description, slicerValue  *string
		brandID                   *uuid.UUID
		productAttrJSON           []byte
	}
	raw := []rawItem{}
	brandIDSet := map[uuid.UUID]struct{}{}
	productIDSet := map[uuid.UUID]struct{}{}
	for rows.Next() {
		var r rawItem
		if err := rows.Scan(&r.itemID, &r.barcode, &r.sku, &r.itemAttrJSON, &r.itemVarJSON,
			&r.productID, &r.categoryID, &r.title, &r.description, &r.modelCode,
			&r.slicerValue, &r.brandID, &r.productAttrJSON); err != nil {
			rows.Close()
			return nil, err
		}
		raw = append(raw, r)
		if r.brandID != nil {
			brandIDSet[*r.brandID] = struct{}{}
		}
		productIDSet[r.productID] = struct{}{}
	}
	rows.Close()
	if err := rows.Err(); err != nil {
		return nil, err
	}
	if len(raw) == 0 {
		return []application.CatalogListingSource{}, nil
	}

	// Marka adları/kodları tek sorguda önbelleklenir.
	brandNames := map[uuid.UUID][2]*string{} // [name, code]
	if len(brandIDSet) > 0 {
		brandIDs := make([]uuid.UUID, 0, len(brandIDSet))
		for id := range brandIDSet {
			brandIDs = append(brandIDs, id)
		}
		brandRows, err := g.pool.Query(ctx,
			`SELECT id, name, code FROM catalog.brands WHERE tenant_id = $1 AND id = ANY($2)`,
			tenantID, brandIDs)
		if err != nil {
			return nil, fmt.Errorf("channels: markalar okunamadı: %w", err)
		}
		for brandRows.Next() {
			var id uuid.UUID
			var name, code *string
			if err := brandRows.Scan(&id, &name, &code); err != nil {
				brandRows.Close()
				return nil, err
			}
			brandNames[id] = [2]*string{name, code}
		}
		brandRows.Close()
		if err := brandRows.Err(); err != nil {
			return nil, err
		}
	}

	// Görseller: ürün başına birincil önce, sonra sıraya göre; tek sorguda çekilir.
	productIDs := make([]uuid.UUID, 0, len(productIDSet))
	for id := range productIDSet {
		productIDs = append(productIDs, id)
	}
	imagesByProduct := map[uuid.UUID][]string{}
	imageRows, err := g.pool.Query(ctx,
		`SELECT product_id, url FROM catalog.product_images
		 WHERE product_id = ANY($1) ORDER BY product_id, is_primary DESC, sort_order`, productIDs)
	if err != nil {
		return nil, fmt.Errorf("channels: ürün görselleri okunamadı: %w", err)
	}
	for imageRows.Next() {
		var productID uuid.UUID
		var url string
		if err := imageRows.Scan(&productID, &url); err != nil {
			imageRows.Close()
			return nil, err
		}
		imagesByProduct[productID] = append(imagesByProduct[productID], url)
	}
	imageRows.Close()
	if err := imageRows.Err(); err != nil {
		return nil, err
	}

	// Tenant başına en fazla bir slicer ekseni olabilir; tüm değerleri tek
	// sorguda çekip etikete göre (duyarsız) çözülür.
	type slicerValue struct {
		variantID, valueID uuid.UUID
		label              string
	}
	slicerByLabel := map[string]slicerValue{}
	slicerRows, err := g.pool.Query(ctx,
		`SELECT v.id, vv.id, vv.label FROM catalog.variants v
		 JOIN catalog.variant_values vv ON vv.variant_id = v.id
		 WHERE v.tenant_id = $1 AND v.slicer = TRUE`, tenantID)
	if err != nil {
		return nil, fmt.Errorf("channels: slicer değerleri okunamadı: %w", err)
	}
	for slicerRows.Next() {
		var sv slicerValue
		if err := slicerRows.Scan(&sv.variantID, &sv.valueID, &sv.label); err != nil {
			slicerRows.Close()
			return nil, err
		}
		slicerByLabel[strings.ToLower(sv.label)] = sv
	}
	slicerRows.Close()
	if err := slicerRows.Err(); err != nil {
		return nil, err
	}

	sources := make([]application.CatalogListingSource, 0, len(raw))
	for _, r := range raw {
		var productAttrs []gatewayAttrValue
		if err := json.Unmarshal(r.productAttrJSON, &productAttrs); err != nil {
			return nil, fmt.Errorf("channels: ürün özellik belgesi çözümlenemedi: %w", err)
		}
		var itemAttrs []gatewayAttrValue
		var itemVars []gatewayVariantValue
		if err := json.Unmarshal(r.itemAttrJSON, &itemAttrs); err != nil {
			return nil, fmt.Errorf("channels: kalem özellik belgesi çözümlenemedi: %w", err)
		}
		if err := json.Unmarshal(r.itemVarJSON, &itemVars); err != nil {
			return nil, fmt.Errorf("channels: kalem eksen belgesi çözümlenemedi: %w", err)
		}

		var brandName, brandCode *string
		if r.brandID != nil {
			if pair, ok := brandNames[*r.brandID]; ok {
				brandName, brandCode = pair[0], pair[1]
			}
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
		if r.slicerValue != nil && strings.TrimSpace(*r.slicerValue) != "" {
			if sv, ok := slicerByLabel[strings.ToLower(strings.TrimSpace(*r.slicerValue))]; ok {
				put(application.CatalogListingSelection{
					IsVariant: true, SourceID: sv.variantID, ValueID: sv.valueID, ValueLabel: sv.label})
			}
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
			ProductItemID: r.itemID, ProductID: r.productID, CategoryID: r.categoryID,
			Title: r.title, Description: r.description, BrandName: brandName,
			BrandExternalCode: brandCode, ModelCode: r.modelCode, Barcode: r.barcode, Sku: r.sku,
			Attributes: selections, ImageURLs: imagesByProduct[r.productID],
		})
	}
	return sources, nil
}
