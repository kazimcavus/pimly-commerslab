// Package infrastructure, Inventory modülünün pgx kalıcılık katmanını içerir.
// Stok değişim olayları inventory.outbox_messages'a stok yazımıyla aynı
// transaction'da düşer (işlemsel outbox).
package infrastructure

import (
	"context"
	"errors"
	"fmt"
	"time"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgxpool"

	"pimly.commerslab/backend-go/internal/modules/inventory/application"
	"pimly.commerslab/backend-go/internal/outbox"
)

// StockLevelRepository, inventory.stock_levels tablosunun pgx uygulamasıdır.
type StockLevelRepository struct {
	pool *pgxpool.Pool
}

// NewStockLevelRepository, verilen havuzla stok deposunu oluşturur.
func NewStockLevelRepository(pool *pgxpool.Pool) *StockLevelRepository {
	return &StockLevelRepository{pool: pool}
}

// GetByItem, kalemin stok kaydını döner; yoksa nil.
func (r *StockLevelRepository) GetByItem(ctx context.Context, tenantID, productItemID uuid.UUID) (*application.StockLevel, error) {
	var s application.StockLevel
	err := r.pool.QueryRow(ctx,
		`SELECT id, product_item_id, quantity, updated_at FROM inventory.stock_levels
		 WHERE tenant_id = $1 AND product_item_id = $2`, tenantID, productItemID).
		Scan(&s.ID, &s.ProductItemID, &s.Quantity, &s.UpdatedAt)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("inventory: stok okunamadı: %w", err)
	}
	return &s, nil
}

// Add, yeni stok kaydını (ve gerekiyorsa değişim olayını) tek transaction'da ekler.
func (r *StockLevelRepository) Add(ctx context.Context, tenantID uuid.UUID, s *application.StockLevel, raiseEvent bool) error {
	return r.write(ctx, tenantID, s, raiseEvent,
		`INSERT INTO inventory.stock_levels (id, product_item_id, quantity, updated_at, tenant_id)
		 VALUES ($1, $2, $3, $4, $5)`,
		s.ID, s.ProductItemID, s.Quantity, s.UpdatedAt, tenantID)
}

// Update, stok kaydını (ve gerekiyorsa değişim olayını) tek transaction'da kalıcılaştırır.
func (r *StockLevelRepository) Update(ctx context.Context, tenantID uuid.UUID, s *application.StockLevel, raiseEvent bool) error {
	return r.write(ctx, tenantID, s, raiseEvent,
		`UPDATE inventory.stock_levels SET quantity = $3, updated_at = $4
		 WHERE tenant_id = $1 AND id = $2`,
		tenantID, s.ID, s.Quantity, s.UpdatedAt)
}

// write, SQL'i ve (istenmişse) StockLevelChanged olayını tek transaction'da yürütür.
func (r *StockLevelRepository) write(ctx context.Context, tenantID uuid.UUID, s *application.StockLevel, raiseEvent bool, sql string, args ...any) error {
	tx, err := r.pool.Begin(ctx)
	if err != nil {
		return fmt.Errorf("inventory: stok işlemi başlatılamadı: %w", err)
	}
	defer func() { _ = tx.Rollback(ctx) }()

	if _, err := tx.Exec(ctx, sql, args...); err != nil {
		return fmt.Errorf("inventory: stok yazılamadı: %w", err)
	}
	if raiseEvent {
		if err := outbox.Write(ctx, tx, "inventory", tenantID, []outbox.Event{
			outbox.StockLevelChanged{ProductItemID: s.ProductItemID, OccurredOnUtc: time.Now().UTC()},
		}); err != nil {
			return err
		}
	}
	return tx.Commit(ctx)
}

// CatalogItemGateway, kalem varlığını Catalog şemasından doğrulayan ACL
// uyarlayıcısıdır (.NET Pimly.Integration gateway'inin süreç içi karşılığı;
// aynı veritabanında şemalar arası salt-okunur sorgu).
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
		return false, fmt.Errorf("inventory: kalem varlığı sorgulanamadı: %w", err)
	}
	return exists, nil
}
