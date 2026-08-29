// pimly-product-imports-worker, pazaryerinden ürün içe aktarma işlerini işler
// (.NET Pimly.ProductImports.Worker karşılığı). channels.product_import_runs
// kuyruğundaki pending işleri FOR UPDATE SKIP LOCKED ile claim eder ve
// productimports.Processor ile uçtan uca yürütür: ürün sayfaları → attribute
// cache tazeleme → plan → kategori/eksen/özellik/eşleme garantileri → grup
// başına ürün + fiyat + stok + görsel + listeleme tohumu.
//
// ProductImports__TenantIds doluysa yalnızca o tenant'ların işleri claim
// edilir (tenant-izole instance); boşsa kuyruk tüm tenant'lar için ortaktır.
// Go dönemi iyileştirmesi: görsel indirme sıralı değil, 4'lük sınırlı havuzla
// paralel yapılır.
package main

import (
	"context"
	"fmt"
	"log/slog"
	"net/http"
	"os"
	"time"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/integration/catalogimport"
	"pimly.commerslab/backend-go/internal/integration/trendyol"
	catalogapp "pimly.commerslab/backend-go/internal/modules/catalog/application"
	cataloginfra "pimly.commerslab/backend-go/internal/modules/catalog/infrastructure"
	channelsapp "pimly.commerslab/backend-go/internal/modules/channels/application"
	"pimly.commerslab/backend-go/internal/modules/channels/application/productimports"
	channelsinfra "pimly.commerslab/backend-go/internal/modules/channels/infrastructure"
	inventoryapp "pimly.commerslab/backend-go/internal/modules/inventory/application"
	inventoryinfra "pimly.commerslab/backend-go/internal/modules/inventory/infrastructure"
	mediaapp "pimly.commerslab/backend-go/internal/modules/media/application"
	mediainfra "pimly.commerslab/backend-go/internal/modules/media/infrastructure"
	pricingapp "pimly.commerslab/backend-go/internal/modules/pricing/application"
	pricinginfra "pimly.commerslab/backend-go/internal/modules/pricing/infrastructure"
	"pimly.commerslab/backend-go/internal/platform/config"
	"pimly.commerslab/backend-go/internal/platform/obs"
	"pimly.commerslab/backend-go/internal/platform/pg"
	"pimly.commerslab/backend-go/internal/platform/worker"
)

func main() {
	if err := run(); err != nil {
		slog.Error("Product imports worker başlatılamadı.", slog.Any("Error", err))
		os.Exit(1)
	}
}

// run, worker yaşam döngüsünü yönetir.
func run() error {
	ctx, stop := worker.Setup("pimly-product-imports-worker")
	defer stop()

	cfg, err := config.Load("pimly-product-imports-worker")
	if err != nil {
		return err
	}
	if cfg.Server.Addr == ":7000" {
		cfg.Server.Addr = ":7003" // worker'ın varsayılan metrik portu API ile çakışmasın
	}

	pool, err := pg.NewPool(ctx, cfg.ConnectionStrings.Database)
	if err != nil {
		return err
	}
	defer pool.Close()

	// Medya depolama dizini hazırlanır: import edilen görseller buraya yazılır.
	if err := os.MkdirAll(cfg.Media.StoragePath, 0o755); err != nil {
		return fmt.Errorf("medya depolama dizini oluşturulamadı: %w", err)
	}

	health := obs.NewHealth(obs.ReadyCheck{Name: "db", Check: func(ctx context.Context) error {
		return pool.Ping(ctx)
	}})
	shutdownMetrics := worker.ServeMetrics(cfg.Server.Addr, health)
	defer func() { _ = shutdownMetrics(context.Background()) }()

	// Catalog/Pricing/Inventory/Media kompozisyonu (API host ile aynı kablolar;
	// import gateway'i bu handler'lara delege eder).
	attributeRepo := cataloginfra.NewAttributeRepository(pool)
	brandRepo := cataloginfra.NewBrandRepository(pool)
	categoryRepo := cataloginfra.NewCategoryRepository(pool)
	variantRepo := cataloginfra.NewVariantRepository(pool)
	productRepo := cataloginfra.NewProductRepository(pool)
	pricingRepo := pricinginfra.NewPricingRepository(pool)
	skuGeneratorHandlers := catalogapp.NewSkuGeneratorHandlers(
		cataloginfra.NewSkuConfigRepository(pool), cataloginfra.NewSkuCounterAllocator(pool))

	gateway := catalogimport.NewGateway(
		pool,
		catalogapp.NewCategoryHandlers(categoryRepo, attributeRepo),
		catalogapp.NewBrandHandlers(brandRepo),
		catalogapp.NewAttributeHandlers(attributeRepo),
		catalogapp.NewVariantHandlers(variantRepo, cfg.Media.AllowedUrlPrefix),
		catalogapp.NewProductHandlers(
			productRepo, categoryRepo, brandRepo, variantRepo, attributeRepo,
			skuGeneratorHandlers, cfg.Media.AllowedUrlPrefix),
		pricingapp.NewPricingHandlers(pricingRepo, pricinginfra.NewCatalogItemGateway(pool)),
		inventoryapp.NewStockHandlers(
			inventoryinfra.NewStockLevelRepository(pool), inventoryinfra.NewCatalogItemGateway(pool)),
		mediaapp.NewUploadHandlers(
			mediainfra.NewLocalBlobStorage(cfg.Media.StoragePath), cfg.Media.PublicBaseUrl),
		categoryRepo, brandRepo, attributeRepo, variantRepo, productRepo, pricingRepo,
		&http.Client{Timeout: 20 * time.Second})

	// Trendyol istemcileri: stub modu yapılandırmayla seçilir; gerçek istemciler
	// ortak rate limiter'lı taban üzerinden konuşur.
	var productsClient productimports.MarketplaceProductsClient = trendyol.StubProductsClient{}
	var attributesClient channelsapp.CategoryAttributesClient = trendyol.StubCategoryAttributesClient{}
	if !cfg.Channels.UseStubTaxonomyClient {
		trendyolClient := trendyol.NewClient(cfg.Channels.TrendyolApiBaseUrl, trendyol.DefaultRateLimits())
		productsClient = trendyol.NewProductsClient(trendyolClient)
		attributesClient = trendyol.NewCategoryAttributesClient(trendyolClient)
	}

	repo := channelsinfra.NewRepository(pool)
	listingRepo := channelsinfra.NewListingRepository(pool)
	processor := productimports.NewProcessor(repo, listingRepo, gateway,
		productsClient, attributesClient, productimports.Options{
			PageSize:                cfg.Channels.ImportPageSize,
			ProgressSaveEveryGroups: 1,
			MaxImagesPerProduct:     cfg.Channels.ImportMaxImagesPerProduct,
		})

	tenantFilter, err := parseTenantFilter(cfg.ProductImports.TenantIds)
	if err != nil {
		return err
	}
	if len(tenantFilter) > 0 {
		slog.Info("Product import worker started with tenant filter.",
			slog.Int("TenantCount", len(tenantFilter)))
	} else {
		slog.Info("Product import worker started for all tenants.")
	}

	queue := &importQueue{repo: repo, processor: processor, tenantFilter: tenantFilter}
	pollInterval := time.Duration(maxInt(1, cfg.ProductImports.PollIntervalSeconds)) * time.Second
	worker.RunLoop(ctx, "product-imports", pollInterval, queue.iterate)
	return nil
}

// importQueue, kuyruk claim + işleme döngüsünü taşır
// (.NET ProductImportBackgroundService karşılığı).
type importQueue struct {
	repo         *channelsinfra.Repository
	processor    *productimports.Processor
	tenantFilter []uuid.UUID
}

// iterate, sıradaki pending işi claim edip işler; iş yoksa false döner.
func (q *importQueue) iterate(ctx context.Context) (bool, error) {
	run, err := q.repo.ClaimNextPendingImportRun(ctx, q.tenantFilter)
	if err != nil {
		return false, err
	}
	if run == nil {
		return false, nil
	}
	slog.Info("Product import run claimed.",
		slog.String("RunId", run.ID.String()),
		slog.String("TenantId", run.TenantID.String()),
		slog.String("Marketplace", run.MarketplaceCode))
	if err := q.processor.Process(ctx, run); err != nil {
		return true, err
	}
	return true, nil
}

// parseTenantFilter, yapılandırmadaki tenant kimliklerini çözer.
func parseTenantFilter(raw []string) ([]uuid.UUID, error) {
	filter := make([]uuid.UUID, 0, len(raw))
	for _, value := range raw {
		id, err := uuid.Parse(value)
		if err != nil {
			return nil, fmt.Errorf("geçersiz ProductImports tenant kimliği %q: %w", value, err)
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
