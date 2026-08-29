// pimly-outbox-worker, modül şemalarının (catalog, pricing, inventory) outbox
// kuyruklarını işleyen dispatcher'dır (.NET Pimly.Outbox.Worker karşılığı).
// Olay → etki eşlemesi:
//
//	catalog.product_item_created.v1   → yalnızca log (yer tutucu)
//	catalog.product_item_deleted.v1   → Pricing fiyatları + Inventory stoğu temizlenir
//	catalog.product_content_changed.v1→ kalemlerin listelemeleri içerik-kirli işaretlenir
//	inventory.stock_level_changed.v1  → listelemeler teklif-kirli işaretlenir
//	pricing.channel_price_changed.v1  → ilgili pazaryerinin listelemesi teklif-kirli işaretlenir
//
// Go dönemi iyileştirmeleri: FOR UPDATE SKIP LOCKED (çoklu instance güvenli),
// üstel backoff (30s·2ⁿ, tavan 1h) ve 10 denemede dead-letter. Eski .NET tip
// adları alias tablosuyla tanınır; cutover sonrası .NET dispatcher'ı bir daha
// çalıştırılmamalıdır (Go adlarını çözemez).
package main

import (
	"context"
	"encoding/json"
	"log/slog"
	"os"
	"time"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"

	channelsinfra "pimly.commerslab/backend-go/internal/modules/channels/infrastructure"
	"pimly.commerslab/backend-go/internal/outbox"
	"pimly.commerslab/backend-go/internal/platform/config"
	"pimly.commerslab/backend-go/internal/platform/obs"
	"pimly.commerslab/backend-go/internal/platform/pg"
	"pimly.commerslab/backend-go/internal/platform/worker"
)

func main() {
	if err := run(); err != nil {
		slog.Error("Outbox worker başlatılamadı.", slog.Any("Error", err))
		os.Exit(1)
	}
}

// run, worker yaşam döngüsünü yönetir.
func run() error {
	ctx, stop := worker.Setup("pimly-outbox-worker")
	defer stop()

	cfg, err := config.Load("pimly-outbox-worker")
	if err != nil {
		return err
	}
	if cfg.Server.Addr == ":7000" {
		cfg.Server.Addr = ":7001" // worker'ın varsayılan metrik portu API ile çakışmasın
	}

	pool, err := pg.NewPool(ctx, cfg.ConnectionStrings.Database)
	if err != nil {
		return err
	}
	defer pool.Close()

	health := obs.NewHealth(obs.ReadyCheck{Name: "db", Check: func(ctx context.Context) error {
		return pool.Ping(ctx)
	}})
	shutdownMetrics := worker.ServeMetrics(cfg.Server.Addr, health)
	defer func() { _ = shutdownMetrics(context.Background()) }()

	listings := channelsinfra.NewListingRepository(pool)
	dispatcher := outbox.NewDispatcher(pool,
		[]string{"catalog", "pricing", "inventory"},
		cfg.Outbox.BatchSize, cfg.Outbox.MaxAttempts)

	registerHandlers(dispatcher, listings)

	pollInterval := time.Duration(cfg.Outbox.PollIntervalSeconds) * time.Second
	worker.RunLoop(ctx, "outbox-dispatcher", pollInterval, dispatcher.ProcessOnce)
	return nil
}

// itemEvent, kalem taşıyan olayların ortak payload biçimidir.
type itemEvent struct {
	ProductItemID uuid.UUID `json:"product_item_id"`
}

// contentChangedEvent, içerik değişim olayının payload biçimidir.
type contentChangedEvent struct {
	ProductID      uuid.UUID   `json:"product_id"`
	ProductItemIds []uuid.UUID `json:"product_item_ids"`
}

// channelPriceEvent, kanal fiyatı olayının payload biçimidir.
type channelPriceEvent struct {
	ProductItemID   uuid.UUID `json:"product_item_id"`
	MarketplaceCode string    `json:"marketplace_code"`
}

// registerHandlers, olay → etki eşlemelerini dispatcher'a bağlar.
func registerHandlers(dispatcher *outbox.Dispatcher, listings *channelsinfra.ListingRepository) {
	// Yeni kalem: yer tutucu — bugün yalnızca loglanır (.NET
	// ProductItemCreatedLoggingHandler karşılığı).
	dispatcher.Register(outbox.EventProductItemCreated,
		func(ctx context.Context, tx pgx.Tx, tenantID uuid.UUID, payload []byte) error {
			var event itemEvent
			if err := json.Unmarshal(payload, &event); err != nil {
				return err
			}
			slog.Info("ProductItemCreated işlendi (yer tutucu).",
				slog.String("ItemId", event.ProductItemID.String()))
			return nil
		})

	// Kalem silindi: uydu context'lerdeki fiyat ve stok kayıtları temizlenir
	// (.NET'te iki ayrı handler; ikisi de aynı transaction'da koşar).
	dispatcher.Register(outbox.EventProductItemDeleted,
		func(ctx context.Context, tx pgx.Tx, tenantID uuid.UUID, payload []byte) error {
			var event itemEvent
			if err := json.Unmarshal(payload, &event); err != nil {
				return err
			}
			for _, sql := range []string{
				`DELETE FROM pricing.product_item_prices WHERE tenant_id = $1 AND product_item_id = $2`,
				`DELETE FROM pricing.base_prices WHERE tenant_id = $1 AND product_item_id = $2`,
				`DELETE FROM pricing.channel_prices WHERE tenant_id = $1 AND product_item_id = $2`,
				`DELETE FROM inventory.stock_levels WHERE tenant_id = $1 AND product_item_id = $2`,
			} {
				if _, err := tx.Exec(ctx, sql, tenantID, event.ProductItemID); err != nil {
					return err
				}
			}
			slog.Info("ProductItemDeleted işlendi: fiyat ve stok kayıtları temizlendi.",
				slog.String("ItemId", event.ProductItemID.String()))
			return nil
		})

	// İçerik değişti: kalemlerin tüm listelemeleri içerik-kirli işaretlenir.
	dispatcher.Register(outbox.EventProductContentChanged,
		func(ctx context.Context, tx pgx.Tx, tenantID uuid.UUID, payload []byte) error {
			var event contentChangedEvent
			if err := json.Unmarshal(payload, &event); err != nil {
				return err
			}
			marked := 0
			now := time.Now().UTC()
			for _, itemID := range event.ProductItemIds {
				count, err := listings.MarkDirtyByItem(ctx, tx, itemID, nil, true, false, now)
				if err != nil {
					return err
				}
				marked += count
			}
			slog.Info("ProductContentChanged işlendi.",
				slog.String("ProductId", event.ProductID.String()), slog.Int("Marked", marked))
			return nil
		})

	// Stok değişti: kalemin tüm listelemeleri teklif-kirli işaretlenir.
	dispatcher.Register(outbox.EventStockLevelChanged,
		func(ctx context.Context, tx pgx.Tx, tenantID uuid.UUID, payload []byte) error {
			var event itemEvent
			if err := json.Unmarshal(payload, &event); err != nil {
				return err
			}
			marked, err := listings.MarkDirtyByItem(ctx, tx, event.ProductItemID, nil, false, true, time.Now().UTC())
			if err != nil {
				return err
			}
			slog.Info("StockLevelChanged işlendi.",
				slog.String("ItemId", event.ProductItemID.String()), slog.Int("Marked", marked))
			return nil
		})

	// Kanal fiyatı değişti: yalnızca ilgili pazaryerinin listelemesi işaretlenir.
	dispatcher.Register(outbox.EventChannelPriceChanged,
		func(ctx context.Context, tx pgx.Tx, tenantID uuid.UUID, payload []byte) error {
			var event channelPriceEvent
			if err := json.Unmarshal(payload, &event); err != nil {
				return err
			}
			marked, err := listings.MarkDirtyByItem(ctx, tx, event.ProductItemID,
				&event.MarketplaceCode, false, true, time.Now().UTC())
			if err != nil {
				return err
			}
			slog.Info("ChannelPriceChanged işlendi.",
				slog.String("ItemId", event.ProductItemID.String()),
				slog.String("Marketplace", event.MarketplaceCode), slog.Int("Marked", marked))
			return nil
		})
}
