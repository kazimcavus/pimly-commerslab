// pimly-listing-sync-worker, kirli listelemelerin fiyat/stok ve içerik
// bilgisini periyodik olarak pazaryerlerine gönderen worker'dır (.NET
// Pimly.ListingSync.Worker karşılığı). İki fazlı desen kullanır: tenant
// bağlamı olmadan bekleyen (tenant, pazaryeri) çiftleri keşfedilir, sonra her
// çift için teklif (ucuz, onaysız) önce, içerik (pahalı, onaya sokar) sonra
// senkronlanır. Poll aralığı aynı zamanda debounce penceresidir — bir
// kalemin ardışık değişimleri tek gönderime iner.
//
// UYARI: Bu worker GERÇEK pazaryeri mağazasına YAZAR (fiyat/stok güncellemesi
// ve ürün kartı gönderimi). ListingSync__UseStubClients=true iken hiçbir HTTP
// isteği Trendyol'a gitmez (StubOfferClient/StubListingClient) — canlıya
// almadan önce mutlaka bu bayrakla doğrulama yapılmalı.
package main

import (
	"context"
	"fmt"
	"log/slog"
	"os"
	"time"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/integration/trendyol"
	channelsapp "pimly.commerslab/backend-go/internal/modules/channels/application"
	"pimly.commerslab/backend-go/internal/modules/channels/application/listingsync"
	channelsinfra "pimly.commerslab/backend-go/internal/modules/channels/infrastructure"
	inventoryinfra "pimly.commerslab/backend-go/internal/modules/inventory/infrastructure"
	pricinginfra "pimly.commerslab/backend-go/internal/modules/pricing/infrastructure"
	"pimly.commerslab/backend-go/internal/platform/config"
	"pimly.commerslab/backend-go/internal/platform/obs"
	"pimly.commerslab/backend-go/internal/platform/pg"
	"pimly.commerslab/backend-go/internal/platform/worker"
)

func main() {
	if err := run(); err != nil {
		slog.Error("Listing sync worker başlatılamadı.", slog.Any("Error", err))
		os.Exit(1)
	}
}

// run, worker yaşam döngüsünü yönetir.
func run() error {
	ctx, stop := worker.Setup("pimly-listing-sync-worker")
	defer stop()

	cfg, err := config.Load("pimly-listing-sync-worker")
	if err != nil {
		return err
	}
	if cfg.Server.Addr == ":7000" {
		cfg.Server.Addr = ":7004" // worker'ın varsayılan metrik portu API ile çakışmasın
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

	store := &channelsStore{
		Repository:        channelsinfra.NewRepository(pool),
		ListingRepository: channelsinfra.NewListingRepository(pool),
	}
	pricingGateway := &pricingChannelPriceAdapter{repo: pricinginfra.NewPricingRepository(pool)}
	inventoryGateway := &inventoryStockAdapter{repo: inventoryinfra.NewStockLevelRepository(pool)}
	catalogSources := &catalogListingSourceAdapter{gateway: channelsinfra.NewCatalogGateway(pool)}

	resolver := listingsync.NewClientResolver()
	code := cfg.Channels.MarketplaceCode
	if cfg.Channels.UseStubTaxonomyClient {
		resolver.RegisterOffer(code, trendyol.StubOfferClient{})
		resolver.RegisterListing(code, trendyol.StubListingClient{})
		slog.Warn("Listing sync worker stub istemcilerle çalışıyor; Trendyol'a HİÇBİR yazma isteği gönderilmeyecek.")
	} else {
		trendyolClient := trendyol.NewClient(cfg.Channels.TrendyolApiBaseUrl, trendyol.DefaultRateLimits())
		resolver.RegisterOffer(code, trendyol.NewOfferClient(trendyolClient))
		resolver.RegisterListing(code, trendyol.NewListingClient(trendyolClient))
		slog.Warn("Listing sync worker GERÇEK Trendyol istemcileriyle çalışıyor; fiyat/stok/ürün kartı canlıya yazılacak.")
	}

	assembler := listingsync.NewAssembler(store)
	offerSyncer := listingsync.NewOfferSyncer(store, pricingGateway, inventoryGateway, resolver)
	contentSyncer := listingsync.NewContentSyncer(store, catalogSources, pricingGateway, inventoryGateway, assembler, resolver)
	runner := listingsync.NewRunner(store, offerSyncer, contentSyncer).
		WithConcurrency(cfg.ListingSync.Concurrency).
		WithMarketplaces(code)
	if cfg.ListingSync.ScopeLockEnabled {
		runner = runner.WithScopeLocker(channelsinfra.NewScopeLocker(pool))
	} else {
		slog.Warn("Kapsam kilidi KAPALI; bu worker'dan yalnızca TEK örnek çalıştırılmalı, " +
			"aksi halde pazaryerine çift gönderim olur.")
	}

	tenantFilter, err := parseTenantFilter(cfg.ListingSync.TenantIds)
	if err != nil {
		return err
	}
	if len(tenantFilter) > 0 {
		slog.Info("Listing sync worker started with tenant filter.", slog.Int("TenantCount", len(tenantFilter)))
	} else {
		slog.Info("Listing sync worker started for all tenants.")
	}

	pollInterval := time.Duration(maxInt(1, cfg.ListingSync.PollIntervalSeconds)) * time.Second
	worker.RunLoop(ctx, "listing-sync", pollInterval, func(ctx context.Context) (bool, error) {
		return runner.RunOnce(ctx, tenantFilter)
	})
	return nil
}

// channelsStore, Repository ve ListingRepository'yi listingsync.Store
// portunda birleştirir (.NET'te tek ChannelsDbContext üzerinden erişilen iki
// yüz; Go'da iki ayrı struct — burada metot tanımlarıyla birleştirilir).
type channelsStore struct {
	*channelsinfra.Repository
	*channelsinfra.ListingRepository
}

// pricingChannelPriceAdapter, PricingRepository'yi listingsync.
// PricingChannelPriceGateway portuna uyarlar.
type pricingChannelPriceAdapter struct {
	repo *pricinginfra.PricingRepository
}

func (a *pricingChannelPriceAdapter) ListForMarketplace(ctx context.Context, tenantID uuid.UUID, marketplaceCode string) ([]listingsync.DecidedChannelPrice, error) {
	prices, err := a.repo.ListChannelPricesByMarketplace(ctx, tenantID, marketplaceCode)
	if err != nil {
		return nil, err
	}
	result := make([]listingsync.DecidedChannelPrice, len(prices))
	for i, p := range prices {
		var compareAt *string
		if p.CompareAtAmount != nil {
			value := string(*p.CompareAtAmount)
			compareAt = &value
		}
		result[i] = listingsync.DecidedChannelPrice{
			ProductItemID: p.ProductItemID, Amount: string(p.Amount),
			CompareAtAmount: compareAt, Currency: p.Currency,
		}
	}
	return result, nil
}

// inventoryStockAdapter, StockLevelRepository'yi listingsync.
// InventoryStockGateway portuna uyarlar.
type inventoryStockAdapter struct {
	repo *inventoryinfra.StockLevelRepository
}

func (a *inventoryStockAdapter) GetQuantities(ctx context.Context, tenantID uuid.UUID, productItemIDs []uuid.UUID) (map[uuid.UUID]int, error) {
	return a.repo.GetQuantitiesByItems(ctx, tenantID, productItemIDs)
}

// catalogListingSourceAdapter, CatalogGateway'i listingsync.
// CatalogListingSourceGateway portuna uyarlar.
type catalogListingSourceAdapter struct {
	gateway *channelsinfra.CatalogGateway
}

func (a *catalogListingSourceAdapter) Get(ctx context.Context, tenantID uuid.UUID, productItemIDs []uuid.UUID) ([]channelsapp.CatalogListingSource, error) {
	return a.gateway.GetListingSourcesByItems(ctx, tenantID, productItemIDs)
}

// parseTenantFilter, yapılandırmadaki tenant kimliklerini çözer.
func parseTenantFilter(raw []string) ([]uuid.UUID, error) {
	filter := make([]uuid.UUID, 0, len(raw))
	for _, value := range raw {
		id, err := uuid.Parse(value)
		if err != nil {
			return nil, fmt.Errorf("geçersiz ListingSync tenant kimliği %q: %w", value, err)
		}
		filter = append(filter, id)
	}
	return filter, nil
}

// maxInt, iki tamsayının büyüğünü döner.
func maxInt(a, b int) int {
	if a > b {
		return a
	}
	return b
}
