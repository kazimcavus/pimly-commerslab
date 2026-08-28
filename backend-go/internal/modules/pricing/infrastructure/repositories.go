// Package infrastructure, Pricing modülünün pgx kalıcılık katmanını içerir.
// Tutarlar numeric(14,2) kolonlarına dizgi olarak yazılır ve ::text ile
// okunur; böylece ölçek (449.90) hiçbir katmanda kaybolmaz. Kanal fiyatı
// değişim olayları pricing.outbox_messages'a aynı transaction'da düşer.
package infrastructure

import (
	"context"
	"errors"
	"fmt"
	"strings"
	"time"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgxpool"

	"pimly.commerslab/backend-go/internal/modules/pricing/application"
	"pimly.commerslab/backend-go/internal/outbox"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// PricingRepository, pricing şeması tablolarının pgx uygulamasıdır.
type PricingRepository struct {
	pool *pgxpool.Pool
}

// NewPricingRepository, verilen havuzla depoyu oluşturur.
func NewPricingRepository(pool *pgxpool.Pool) *PricingRepository {
	return &PricingRepository{pool: pool}
}

// --- fiyat tanımları ---

// GetDefinition, kimlikle tanımı döner; yoksa nil.
func (r *PricingRepository) GetDefinition(ctx context.Context, tenantID, id uuid.UUID) (*application.PriceDefinition, error) {
	var d application.PriceDefinition
	err := r.pool.QueryRow(ctx,
		`SELECT id, name, code FROM pricing.price_definitions WHERE tenant_id = $1 AND id = $2`,
		tenantID, id).Scan(&d.ID, &d.Name, &d.Code)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("pricing: tanım okunamadı: %w", err)
	}
	return &d, nil
}

// GetDefinitionByName, tanımı ada göre büyük/küçük harf duyarsız döner; yoksa nil.
func (r *PricingRepository) GetDefinitionByName(ctx context.Context, tenantID uuid.UUID, name string) (*application.PriceDefinition, error) {
	var d application.PriceDefinition
	err := r.pool.QueryRow(ctx,
		`SELECT id, name, code FROM pricing.price_definitions
		 WHERE tenant_id = $1 AND name ILIKE $2 LIMIT 1`,
		tenantID, strings.TrimSpace(name)).Scan(&d.ID, &d.Name, &d.Code)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("pricing: tanım okunamadı: %w", err)
	}
	return &d, nil
}

// ListDefinitions, tanımları ada göre sıralı ve sayfalanmış döner.
func (r *PricingRepository) ListDefinitions(ctx context.Context, tenantID uuid.UUID, p sharedkernel.Pagination) (sharedkernel.PagedResult[*application.PriceDefinition], error) {
	var total int
	if err := r.pool.QueryRow(ctx,
		`SELECT count(*) FROM pricing.price_definitions WHERE tenant_id = $1`, tenantID).Scan(&total); err != nil {
		return sharedkernel.PagedResult[*application.PriceDefinition]{}, fmt.Errorf("pricing: tanımlar sayılamadı: %w", err)
	}
	rows, err := r.pool.Query(ctx,
		`SELECT id, name, code FROM pricing.price_definitions
		 WHERE tenant_id = $1 ORDER BY name OFFSET $2 LIMIT $3`,
		tenantID, p.Skip(), p.PageSize)
	if err != nil {
		return sharedkernel.PagedResult[*application.PriceDefinition]{}, fmt.Errorf("pricing: tanımlar listelenemedi: %w", err)
	}
	defer rows.Close()

	items := []*application.PriceDefinition{}
	for rows.Next() {
		var d application.PriceDefinition
		if err := rows.Scan(&d.ID, &d.Name, &d.Code); err != nil {
			return sharedkernel.PagedResult[*application.PriceDefinition]{}, err
		}
		items = append(items, &d)
	}
	if err := rows.Err(); err != nil {
		return sharedkernel.PagedResult[*application.PriceDefinition]{}, err
	}
	return sharedkernel.NewPagedResult(items, p, total), nil
}

// AddDefinition, yeni tanımı ekler.
func (r *PricingRepository) AddDefinition(ctx context.Context, tenantID uuid.UUID, d *application.PriceDefinition) error {
	_, err := r.pool.Exec(ctx,
		`INSERT INTO pricing.price_definitions (id, name, code, tenant_id) VALUES ($1, $2, $3, $4)`,
		d.ID, d.Name, d.Code, tenantID)
	if err != nil {
		return fmt.Errorf("pricing: tanım eklenemedi: %w", err)
	}
	return nil
}

// UpdateDefinition, tanımı kalıcılaştırır.
func (r *PricingRepository) UpdateDefinition(ctx context.Context, tenantID uuid.UUID, d *application.PriceDefinition) error {
	_, err := r.pool.Exec(ctx,
		`UPDATE pricing.price_definitions SET name = $3, code = $4 WHERE tenant_id = $1 AND id = $2`,
		tenantID, d.ID, d.Name, d.Code)
	if err != nil {
		return fmt.Errorf("pricing: tanım güncellenemedi: %w", err)
	}
	return nil
}

// RemoveDefinition, tanımı siler (kalem fiyatları cascade silinir).
func (r *PricingRepository) RemoveDefinition(ctx context.Context, tenantID, id uuid.UUID) error {
	_, err := r.pool.Exec(ctx,
		`DELETE FROM pricing.price_definitions WHERE tenant_id = $1 AND id = $2`, tenantID, id)
	if err != nil {
		return fmt.Errorf("pricing: tanım silinemedi: %w", err)
	}
	return nil
}

// --- kalem fiyatları ---

// ListItemPrices, kalemin fiyatlarını tanım adıyla join edip tanım kimliğine
// göre sıralı döner (.NET ItemPriceRepository ile aynı sıra).
func (r *PricingRepository) ListItemPrices(ctx context.Context, tenantID, productItemID uuid.UUID) ([]application.ItemPriceRow, error) {
	rows, err := r.pool.Query(ctx,
		`SELECT p.id, p.product_item_id, p.price_definition_id, d.name,
		        p.amount::text, p.currency, p.updated_at
		 FROM pricing.product_item_prices p
		 JOIN pricing.price_definitions d ON d.id = p.price_definition_id AND d.tenant_id = p.tenant_id
		 WHERE p.tenant_id = $1 AND p.product_item_id = $2
		 ORDER BY p.price_definition_id`, tenantID, productItemID)
	if err != nil {
		return nil, fmt.Errorf("pricing: kalem fiyatları listelenemedi: %w", err)
	}
	defer rows.Close()

	items := []application.ItemPriceRow{}
	for rows.Next() {
		var row application.ItemPriceRow
		var amount string
		if err := rows.Scan(&row.ID, &row.ProductItemID, &row.PriceDefinitionID, &row.DefinitionName,
			&amount, &row.Currency, &row.UpdatedAt); err != nil {
			return nil, err
		}
		row.Amount = application.Decimal(amount)
		items = append(items, row)
	}
	return items, rows.Err()
}

// GetItemPrice, kalem × tanım fiyatını döner; yoksa nil.
func (r *PricingRepository) GetItemPrice(ctx context.Context, tenantID, productItemID, definitionID uuid.UUID) (*application.ItemPrice, error) {
	var p application.ItemPrice
	var amount string
	err := r.pool.QueryRow(ctx,
		`SELECT id, product_item_id, price_definition_id, amount::text, currency, updated_at
		 FROM pricing.product_item_prices
		 WHERE tenant_id = $1 AND product_item_id = $2 AND price_definition_id = $3`,
		tenantID, productItemID, definitionID).
		Scan(&p.ID, &p.ProductItemID, &p.PriceDefinitionID, &amount, &p.Currency, &p.UpdatedAt)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("pricing: kalem fiyatı okunamadı: %w", err)
	}
	p.Amount = application.Decimal(amount)
	return &p, nil
}

// UpsertItemPrice, kalem fiyatını ekler/günceller.
func (r *PricingRepository) UpsertItemPrice(ctx context.Context, tenantID uuid.UUID, p *application.ItemPrice) error {
	_, err := r.pool.Exec(ctx,
		`INSERT INTO pricing.product_item_prices
		   (id, product_item_id, price_definition_id, amount, currency, updated_at, tenant_id)
		 VALUES ($1, $2, $3, $4::numeric, $5, $6, $7)
		 ON CONFLICT (id) DO UPDATE SET amount = $4::numeric, currency = $5, updated_at = $6`,
		p.ID, p.ProductItemID, p.PriceDefinitionID, string(p.Amount), p.Currency, p.UpdatedAt, tenantID)
	if err != nil {
		return fmt.Errorf("pricing: kalem fiyatı yazılamadı: %w", err)
	}
	return nil
}

// RemoveItemPrice, kalem fiyatını siler; silinen satır olup olmadığını döner.
func (r *PricingRepository) RemoveItemPrice(ctx context.Context, tenantID, productItemID, definitionID uuid.UUID) (bool, error) {
	tag, err := r.pool.Exec(ctx,
		`DELETE FROM pricing.product_item_prices
		 WHERE tenant_id = $1 AND product_item_id = $2 AND price_definition_id = $3`,
		tenantID, productItemID, definitionID)
	if err != nil {
		return false, fmt.Errorf("pricing: kalem fiyatı silinemedi: %w", err)
	}
	return tag.RowsAffected() > 0, nil
}

// --- temel fiyat ---

// GetBasePrice, kalemin temel fiyatını döner; yoksa nil.
func (r *PricingRepository) GetBasePrice(ctx context.Context, tenantID, productItemID uuid.UUID) (*application.BasePrice, error) {
	var p application.BasePrice
	var amount string
	var compareAt *string
	err := r.pool.QueryRow(ctx,
		`SELECT id, product_item_id, amount::text, compare_at_amount::text, currency, updated_at
		 FROM pricing.base_prices WHERE tenant_id = $1 AND product_item_id = $2`,
		tenantID, productItemID).
		Scan(&p.ID, &p.ProductItemID, &amount, &compareAt, &p.Currency, &p.UpdatedAt)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("pricing: temel fiyat okunamadı: %w", err)
	}
	p.Amount = application.Decimal(amount)
	if compareAt != nil {
		value := application.Decimal(*compareAt)
		p.CompareAtAmount = &value
	}
	return &p, nil
}

// UpsertBasePrice, temel fiyatı ekler/günceller.
func (r *PricingRepository) UpsertBasePrice(ctx context.Context, tenantID uuid.UUID, p *application.BasePrice) error {
	var compareAt *string
	if p.CompareAtAmount != nil {
		value := string(*p.CompareAtAmount)
		compareAt = &value
	}
	_, err := r.pool.Exec(ctx,
		`INSERT INTO pricing.base_prices
		   (id, product_item_id, amount, compare_at_amount, currency, updated_at, tenant_id)
		 VALUES ($1, $2, $3::numeric, $4::numeric, $5, $6, $7)
		 ON CONFLICT (id) DO UPDATE SET amount = $3::numeric, compare_at_amount = $4::numeric,
		   currency = $5, updated_at = $6`,
		p.ID, p.ProductItemID, string(p.Amount), compareAt, p.Currency, p.UpdatedAt, tenantID)
	if err != nil {
		return fmt.Errorf("pricing: temel fiyat yazılamadı: %w", err)
	}
	return nil
}

// --- kanal fiyatları ---

// scanChannelPrice, tek kanal fiyatı satırını okur.
func scanChannelPrice(row pgx.Row) (*application.ChannelPrice, error) {
	var p application.ChannelPrice
	var amount string
	var compareAt *string
	err := row.Scan(&p.ID, &p.ProductItemID, &p.MarketplaceCode, &amount, &compareAt, &p.Currency, &p.UpdatedAt)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("pricing: kanal fiyatı okunamadı: %w", err)
	}
	p.Amount = application.Decimal(amount)
	if compareAt != nil {
		value := application.Decimal(*compareAt)
		p.CompareAtAmount = &value
	}
	return &p, nil
}

const channelPriceColumns = `id, product_item_id, marketplace_code, amount::text,
	compare_at_amount::text, currency, updated_at`

// ListChannelPrices, kalemin kanal fiyatlarını pazaryerine göre sıralı döner.
func (r *PricingRepository) ListChannelPrices(ctx context.Context, tenantID, productItemID uuid.UUID) ([]*application.ChannelPrice, error) {
	rows, err := r.pool.Query(ctx,
		`SELECT `+channelPriceColumns+` FROM pricing.channel_prices
		 WHERE tenant_id = $1 AND product_item_id = $2 ORDER BY marketplace_code`,
		tenantID, productItemID)
	if err != nil {
		return nil, fmt.Errorf("pricing: kanal fiyatları listelenemedi: %w", err)
	}
	defer rows.Close()

	items := []*application.ChannelPrice{}
	for rows.Next() {
		price, err := scanChannelPrice(rows)
		if err != nil {
			return nil, err
		}
		items = append(items, price)
	}
	return items, rows.Err()
}

// GetChannelPrice, kalemin bir pazaryerindeki fiyatını döner; yoksa nil.
func (r *PricingRepository) GetChannelPrice(ctx context.Context, tenantID, productItemID uuid.UUID, marketplaceCode string) (*application.ChannelPrice, error) {
	return scanChannelPrice(r.pool.QueryRow(ctx,
		`SELECT `+channelPriceColumns+` FROM pricing.channel_prices
		 WHERE tenant_id = $1 AND product_item_id = $2 AND marketplace_code = $3`,
		tenantID, productItemID, marketplaceCode))
}

// UpsertChannelPrice, kanal fiyatını ekler/günceller; raiseEvent true ise
// ChannelPriceChanged olayı aynı transaction'da outbox'a yazılır.
func (r *PricingRepository) UpsertChannelPrice(ctx context.Context, tenantID uuid.UUID, p *application.ChannelPrice, raiseEvent bool) error {
	tx, err := r.pool.Begin(ctx)
	if err != nil {
		return fmt.Errorf("pricing: kanal fiyatı işlemi başlatılamadı: %w", err)
	}
	defer func() { _ = tx.Rollback(ctx) }()

	var compareAt *string
	if p.CompareAtAmount != nil {
		value := string(*p.CompareAtAmount)
		compareAt = &value
	}
	if _, err := tx.Exec(ctx,
		`INSERT INTO pricing.channel_prices
		   (id, product_item_id, marketplace_code, amount, compare_at_amount, currency, updated_at, tenant_id)
		 VALUES ($1, $2, $3, $4::numeric, $5::numeric, $6, $7, $8)
		 ON CONFLICT (id) DO UPDATE SET amount = $4::numeric, compare_at_amount = $5::numeric,
		   currency = $6, updated_at = $7`,
		p.ID, p.ProductItemID, p.MarketplaceCode, string(p.Amount), compareAt,
		p.Currency, p.UpdatedAt, tenantID); err != nil {
		return fmt.Errorf("pricing: kanal fiyatı yazılamadı: %w", err)
	}
	if raiseEvent {
		if err := outbox.Write(ctx, tx, "pricing", tenantID, []outbox.Event{
			outbox.ChannelPriceChanged{
				ProductItemID: p.ProductItemID, MarketplaceCode: p.MarketplaceCode,
				OccurredOnUtc: time.Now().UTC()},
		}); err != nil {
			return err
		}
	}
	return tx.Commit(ctx)
}

// CatalogItemGateway, kalem varlığını Catalog şemasından doğrulayan ACL uyarlayıcısıdır.
type CatalogItemGateway struct {
	pool *pgxpool.Pool
}

// NewCatalogItemGateway, verilen havuzla gateway'i oluşturur.
func NewCatalogItemGateway(pool *pgxpool.Pool) *CatalogItemGateway {
	return &CatalogItemGateway{pool: pool}
}

// Exists, kalemin bu tenant'ta var olup olmadığını döner.
func (g *CatalogItemGateway) Exists(ctx context.Context, tenantID, productItemID uuid.UUID) (bool, error) {
	var exists bool
	err := g.pool.QueryRow(ctx,
		`SELECT EXISTS (SELECT 1 FROM catalog.product_items WHERE tenant_id = $1 AND id = $2)`,
		tenantID, productItemID).Scan(&exists)
	if err != nil {
		return false, fmt.Errorf("pricing: kalem varlığı sorgulanamadı: %w", err)
	}
	return exists, nil
}
