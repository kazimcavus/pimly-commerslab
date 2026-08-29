// Package infrastructure, Inventory modülünün pgx kalıcılık katmanını içerir.
// Stok değişim olayları inventory.outbox_messages'a stok yazımıyla aynı
// transaction'da düşer (işlemsel outbox).
package infrastructure

import (
	"context"
	"errors"
	"fmt"
	"sync"
	"time"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgxpool"

	"pimly.commerslab/backend-go/internal/modules/inventory/application"
	"pimly.commerslab/backend-go/internal/outbox"
)

// DefaultLocationCode, tenant'ın otomatik açılan ana deposunun kodudur.
const DefaultLocationCode = "MAIN"

// StockLevelRepository, inventory.stock_levels tablosunun pgx uygulamasıdır.
//
// Stok artık lokasyon başına tutulur (Shopify çok lokasyonlu çalışır). Dışarıya
// açık sözleşme değişmedi: tek lokasyonlu kullanımda yazma/okuma tenant'ın
// varsayılan deposuna gider, kanala gönderilen miktar ise tüm depoların
// TOPLAMIDIR — böylece ikinci depo eklendiğinde davranış kendiliğinden doğru olur.
type StockLevelRepository struct {
	pool *pgxpool.Pool

	// defaultLocations, tenant → varsayılan lokasyon kimliği önbelleğidir.
	// Varsayılan lokasyon bir kez oluşup değişmediği için (kısmi benzersiz
	// indeks garanti eder) önbellek bayatlayamaz.
	defaultLocations sync.Map
}

// NewStockLevelRepository, verilen havuzla stok deposunu oluşturur.
func NewStockLevelRepository(pool *pgxpool.Pool) *StockLevelRepository {
	return &StockLevelRepository{pool: pool}
}

// DefaultLocationID, tenant'ın varsayılan deposunu döner; yoksa oluşturur.
// Eşzamanlı çağrılarda ON CONFLICT sayesinde tek satır kalır.
func (r *StockLevelRepository) DefaultLocationID(ctx context.Context, tenantID uuid.UUID) (uuid.UUID, error) {
	if cached, ok := r.defaultLocations.Load(tenantID); ok {
		return cached.(uuid.UUID), nil
	}

	if _, err := r.pool.Exec(ctx,
		`INSERT INTO inventory.locations (id, tenant_id, code, name, is_default)
		 VALUES ($1, $2, $3, 'Ana Depo', true)
		 ON CONFLICT (tenant_id, code) DO NOTHING`,
		uuid.New(), tenantID, DefaultLocationCode); err != nil {
		return uuid.Nil, fmt.Errorf("inventory: varsayılan depo oluşturulamadı: %w", err)
	}

	var id uuid.UUID
	if err := r.pool.QueryRow(ctx,
		`SELECT id FROM inventory.locations WHERE tenant_id = $1 AND is_default`, tenantID).
		Scan(&id); err != nil {
		return uuid.Nil, fmt.Errorf("inventory: varsayılan depo okunamadı: %w", err)
	}
	r.defaultLocations.Store(tenantID, id)
	return id, nil
}

// GetByItem, kalemin varsayılan depodaki stok kaydını döner; yoksa nil.
func (r *StockLevelRepository) GetByItem(ctx context.Context, tenantID, productItemID uuid.UUID) (*application.StockLevel, error) {
	locationID, err := r.DefaultLocationID(ctx, tenantID)
	if err != nil {
		return nil, err
	}
	var s application.StockLevel
	err = r.pool.QueryRow(ctx,
		`SELECT id, product_item_id, quantity, updated_at FROM inventory.stock_levels
		 WHERE tenant_id = $1 AND product_item_id = $2 AND location_id = $3`,
		tenantID, productItemID, locationID).
		Scan(&s.ID, &s.ProductItemID, &s.Quantity, &s.UpdatedAt)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("inventory: stok okunamadı: %w", err)
	}
	return &s, nil
}

// GetQuantitiesByItems, verilen kalemlerin stok miktarlarını toplu döner
// (.NET IInventoryStockGateway.GetQuantitiesAsync → IStockLevelRepository.
// ListByItemsAsync portu). Kaydı olmayan kalemler sonuçta yer almaz; çağıran
// taraf eksik kalemi 0 sayar (kalem tükenmiş kabul edilir).
func (r *StockLevelRepository) GetQuantitiesByItems(ctx context.Context, tenantID uuid.UUID, productItemIDs []uuid.UUID) (map[uuid.UUID]int, error) {
	result := map[uuid.UUID]int{}
	if len(productItemIDs) == 0 {
		return result, nil
	}
	// Kanala gönderilen miktar tüm depoların toplamıdır. Bugün tek depo var,
	// yani sonuç değişmiyor; ikinci depo eklendiğinde doğru davranış kendiliğinden gelir.
	rows, err := r.pool.Query(ctx,
		`SELECT product_item_id, SUM(quantity)::int FROM inventory.stock_levels
		 WHERE tenant_id = $1 AND product_item_id = ANY($2)
		 GROUP BY product_item_id`, tenantID, productItemIDs)
	if err != nil {
		return nil, fmt.Errorf("inventory: stok miktarları okunamadı: %w", err)
	}
	defer rows.Close()
	for rows.Next() {
		var itemID uuid.UUID
		var quantity int
		if err := rows.Scan(&itemID, &quantity); err != nil {
			return nil, err
		}
		result[itemID] = quantity
	}
	return result, rows.Err()
}

// Add, yeni stok kaydını (ve gerekiyorsa değişim olayını) tek transaction'da
// tenant'ın varsayılan deposuna ekler.
func (r *StockLevelRepository) Add(ctx context.Context, tenantID uuid.UUID, s *application.StockLevel, raiseEvent bool) error {
	locationID, err := r.DefaultLocationID(ctx, tenantID)
	if err != nil {
		return err
	}
	return r.write(ctx, tenantID, s, raiseEvent,
		`INSERT INTO inventory.stock_levels (id, product_item_id, quantity, updated_at, tenant_id, location_id)
		 VALUES ($1, $2, $3, $4, $5, $6)`,
		s.ID, s.ProductItemID, s.Quantity, s.UpdatedAt, tenantID, locationID)
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
