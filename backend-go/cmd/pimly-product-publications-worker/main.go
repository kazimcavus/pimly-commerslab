// pimly-product-publications-worker, tenant'ın satılabilir kalemlerini bir
// pazaryerine ilk kez göndeme (yayın/publish) işlerini işler (.NET
// Pimly.ProductPublications.Worker karşılığı). channels.
// product_publication_runs kuyruğundaki pending işleri FOR UPDATE SKIP LOCKED
// ile claim eder ve publications.Processor ile yürütür: eşlenmiş
// kategorilerdeki henüz listelenmemiş kalemler için listeleme kaydı açar,
// teslimatı listing-sync ile aynı içerik senkron akışına (listingsync.
// ContentSyncer) bırakır — delta/hash/backoff mantığı tek yerde kalır.
//
// ProductPublications__TenantIds doluysa yalnızca o tenant'ların işleri
// claim edilir; boşsa kuyruk tüm tenant'lar için ortaktır.
//
// UYARI: Bu worker de (listing-sync gibi) GERÇEK pazaryeri mağazasına YAZAR —
// açılan listelemeler içerik senkron turunda pazaryerine gönderilir.
// ListingSync__UseStubClients davranışı Channels__UseStubTaxonomyClient
// bayrağıyla paylaşılır; canlıya almadan önce mutlaka bu bayrakla doğrulama
// yapılmalı.
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
	"pimly.commerslab/backend-go/internal/modules/channels/application/publications"
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
		slog.Error("Product publications worker başlatılamadı.", slog.Any("Error", err))
		os.Exit(1)
	}
}

// run, worker yaşam döngüsünü yönetir.
func run() error {
	ctx, stop := worker.Setup("pimly-product-publications-worker")
	defer stop()

	cfg, err := config.Load("pimly-product-publications-worker")
	if err != nil {
		return err
	}
	if cfg.Server.Addr == ":7000" {
		cfg.Server.Addr = ":7005" // worker'ın varsayılan metrik portu API ile çakışmasın
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

	repo := channelsinfra.NewRepository(pool)
	listingRepo := channelsinfra.NewListingRepository(pool)
	catalogGateway := channelsinfra.NewCatalogGateway(pool)

	// Teslimat, listing-sync ile aynı içerik senkron akışını kullanır
	// (.NET ISyncListingContentHandler karşılığı) — delta/hash/backoff mantığı
	// tek yerde kalır.
	store := &channelsStore{Repository: repo, ListingRepository: listingRepo}
	pricingGateway := &pricingChannelPriceAdapter{repo: pricinginfra.NewPricingRepository(pool)}
	inventoryGateway := &inventoryStockAdapter{repo: inventoryinfra.NewStockLevelRepository(pool)}
	catalogSources := &catalogListingSourceAdapter{gateway: catalogGateway}

	resolver := listingsync.NewClientResolver()
	code := cfg.Channels.MarketplaceCode
	if cfg.Channels.UseStubTaxonomyClient {
		resolver.RegisterListing(code, trendyol.StubListingClient{})
		slog.Warn("Product publications worker stub istemciyle çalışıyor; Trendyol'a HİÇBİR yazma isteği gönderilmeyecek.")
	} else {
		trendyolClient := trendyol.NewClient(cfg.Channels.TrendyolApiBaseUrl, trendyol.DefaultRateLimits())
		resolver.RegisterListing(code, trendyol.NewListingClient(trendyolClient))
		slog.Warn("Product publications worker GERÇEK Trendyol istemcisiyle çalışıyor; yeni ürün kartları canlıya gönderilecek.")
	}

	assembler := listingsync.NewAssembler(store)
	contentSyncer := listingsync.NewContentSyncer(store, catalogSources, pricingGateway, inventoryGateway, assembler, resolver)
	processor := publications.NewProcessor(repo, catalogGateway, listingRepo, contentSyncer)

	tenantFilter, err := parseTenantFilter(cfg.ProductPublications.TenantIds)
	if err != nil {
		return err
	}
	if len(tenantFilter) > 0 {
		slog.Info("Product publications worker started with tenant filter.", slog.Int("TenantCount", len(tenantFilter)))
	} else {
		slog.Info("Product publications worker started for all tenants.")
	}

	queue := &publicationQueue{repo: repo, processor: processor, tenantFilter: tenantFilter}
	pollInterval := time.Duration(maxInt(1, cfg.ProductPublications.PollIntervalSeconds)) * time.Second
	worker.RunLoop(ctx, "product-publications", pollInterval, queue.iterate)
	return nil
}

// publicationQueue, kuyruk claim + işleme döngüsünü taşır
// (.NET ProductPublicationBackgroundService karşılığı).
type publicationQueue struct {
	repo         *channelsinfra.Repository
	processor    *publications.Processor
	tenantFilter []uuid.UUID
}

// iterate, sıradaki pending işi claim edip işler; iş yoksa false döner.
func (q *publicationQueue) iterate(ctx context.Context) (bool, error) {
	run, err := q.repo.ClaimNextPendingPublicationRun(ctx, q.tenantFilter)
	if err != nil {
		return false, err
	}
	if run == nil {
		return false, nil
	}
	slog.Info("Product publication run claimed.",
		slog.String("RunId", run.ID.String()), slog.String("TenantId", run.TenantID.String()),
		slog.String("Marketplace", run.MarketplaceCode))
	if err := q.processor.Process(ctx, run); err != nil {
		return true, err
	}
	return true, nil
}

// --- listingsync port uyarlayıcıları ---
//
// Bu tipler pimly-listing-sync-worker/main.go'dakiyle birebir aynıdır; her
// worker kendi composition root'unu kurar (.NET'in host başına DI grafiği
// kurmasının Go karşılığı) — cmd/ paketleri birbirini import edemediğinden
// küçük bir kod tekrarı kabul edilmiştir.

// channelsStore, Repository ve ListingRepository'yi listingsync.Store
// portunda birleştirir.
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
			return nil, fmt.Errorf("geçersiz ProductPublications tenant kimliği %q: %w", value, err)
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
