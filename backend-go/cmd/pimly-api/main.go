// pimly-api, Pimly'nin HTTP API host'udur (.NET Pimly.Api'nin Go karşılığı).
// Tüm modüllerin uçlarını /api/v1/<modül> önekleri altında tek süreçte sunar;
// başlangıçta şema migration'larını uygular, /healthz /ready /metrics uçlarını
// ve /media statik dosya sunumunu kurar. Kapanışta önce /ready 503'e düşürülür
// (yük dengeleyici drenajı), sonra sunucu 15 saniyelik bütçeyle kapatılır.
package main

import (
	"context"
	"errors"
	"fmt"
	"log/slog"
	"net/http"
	"os"
	"os/signal"
	"path/filepath"
	"syscall"
	"time"

	"github.com/go-chi/chi/v5"
	"go.opentelemetry.io/contrib/instrumentation/net/http/otelhttp"

	catalogapi "pimly.commerslab/backend-go/internal/modules/catalog/api"
	catalogapp "pimly.commerslab/backend-go/internal/modules/catalog/application"
	cataloginfra "pimly.commerslab/backend-go/internal/modules/catalog/infrastructure"
	identityapi "pimly.commerslab/backend-go/internal/modules/identity/api"
	identityapp "pimly.commerslab/backend-go/internal/modules/identity/application"
	identityinfra "pimly.commerslab/backend-go/internal/modules/identity/infrastructure"
	"pimly.commerslab/backend-go/internal/platform/clog"
	"pimly.commerslab/backend-go/internal/platform/config"
	"pimly.commerslab/backend-go/internal/platform/httpx"
	"pimly.commerslab/backend-go/internal/platform/obs"
	"pimly.commerslab/backend-go/internal/platform/pg"
)

func main() {
	if err := run(); err != nil {
		slog.Error("API başlatılamadı.", slog.Any("Error", err))
		os.Exit(1)
	}
}

// run, host'un tüm yaşam döngüsünü yönetir; hata dönerse süreç 1 koduyla çıkar.
func run() error {
	ctx, stop := signal.NotifyContext(context.Background(), syscall.SIGINT, syscall.SIGTERM)
	defer stop()

	cfg, err := config.Load("pimly-api")
	if err != nil {
		return err
	}

	environment := os.Getenv("PIMLY_ENVIRONMENT")
	if environment == "" {
		environment = "Development"
	}
	clog.SetDefault(clog.Options{
		Service:     cfg.Observability.ServiceName,
		Environment: environment,
		Level:       slog.LevelInfo,
	})

	shutdownTracing, err := obs.SetupTracing(ctx, cfg.Observability)
	if err != nil {
		return err
	}
	defer func() { _ = shutdownTracing(context.Background()) }()

	// Şema migration'ları .NET Program.cs ile aynı sırada uygulanır.
	if cfg.Catalog.AutoMigrate {
		if err := pg.Migrate(ctx, cfg.ConnectionStrings.Database, "catalog"); err != nil {
			return err
		}
	}
	for _, schema := range []string{"pricing", "inventory"} {
		if err := pg.Migrate(ctx, cfg.ConnectionStrings.Database, schema); err != nil {
			return err
		}
	}
	if cfg.Channels.AutoMigrate {
		if err := pg.Migrate(ctx, cfg.ConnectionStrings.Database, "channels"); err != nil {
			return err
		}
	}
	if cfg.Identity.AutoMigrate {
		if err := pg.Migrate(ctx, cfg.ConnectionStrings.Identity, "identity"); err != nil {
			return err
		}
	}

	pool, err := pg.NewPool(ctx, cfg.ConnectionStrings.Database)
	if err != nil {
		return err
	}
	defer pool.Close()

	// Medya depolama dizini başlangıçta hazırlanır (.NET Program.cs davranışı).
	if err := os.MkdirAll(cfg.Media.StoragePath, 0o755); err != nil {
		return fmt.Errorf("medya depolama dizini oluşturulamadı: %w", err)
	}

	health := obs.NewHealth(
		obs.ReadyCheck{Name: "catalog-db", Check: func(ctx context.Context) error { return pool.Ping(ctx) }},
		obs.ReadyCheck{Name: "media-storage", Check: mediaStorageCheck(cfg.Media.StoragePath)},
	)

	// Modül kompozisyonu (.NET'teki Add*Application/Add*Infrastructure karşılığı).
	identityStore := identityinfra.NewStore(pool)
	tokenService := identityinfra.NewJwtTokenService(cfg.Identity.Jwt.Secret, cfg.Identity.Jwt.ExpirationHours)
	identityHandlers := identityapi.Handlers{
		Login:    identityapp.NewLoginHandler(identityStore, identityStore, identityinfra.TenantRepo{Store: identityStore}, tokenService),
		Register: identityapp.NewRegisterUserHandler(identityStore, identityStore, tokenService, nil),
		GetMe:    identityapp.NewGetMeHandler(identityStore, identityinfra.TenantRepo{Store: identityStore}, identityStore),
	}

	// Geliştirme ortamında sabit kullanıcı seed'lenir (.NET SeedIdentityDevUserAsync).
	if environment == "Development" {
		if err := identityinfra.SeedDevUser(ctx, identityHandlers.Register); err != nil {
			return fmt.Errorf("geliştirme kullanıcısı seed'lenemedi: %w", err)
		}
	}

	attributeRepo := cataloginfra.NewAttributeRepository(pool)
	brandRepo := cataloginfra.NewBrandRepository(pool)
	categoryRepo := cataloginfra.NewCategoryRepository(pool)
	variantRepo := cataloginfra.NewVariantRepository(pool)
	skuGeneratorHandlers := catalogapp.NewSkuGeneratorHandlers(
		cataloginfra.NewSkuConfigRepository(pool), cataloginfra.NewSkuCounterAllocator(pool))
	catalogHandlers := catalogapi.Handlers{
		Brands:     catalogapp.NewBrandHandlers(brandRepo),
		Categories: catalogapp.NewCategoryHandlers(categoryRepo, attributeRepo),
		Attributes: catalogapp.NewAttributeHandlers(attributeRepo),
		Variants:   catalogapp.NewVariantHandlers(variantRepo, cfg.Media.AllowedUrlPrefix),
		Products: catalogapp.NewProductHandlers(
			cataloginfra.NewProductRepository(pool), categoryRepo, brandRepo,
			variantRepo, attributeRepo, skuGeneratorHandlers, cfg.Media.AllowedUrlPrefix),
		SkuGenerator: skuGeneratorHandlers,
		Barcodes:     catalogapp.NewBarcodeHandlers(cataloginfra.NewBarcodeRepository(pool)),
		Settings:     catalogapp.NewCatalogSettingsHandlers(cataloginfra.NewCatalogSettingsRepository(pool)),
	}

	router := buildRouter(cfg, health, identityHandlers, catalogHandlers)
	server := &http.Server{
		Addr:              cfg.Server.Addr,
		Handler:           router,
		ReadHeaderTimeout: 10 * time.Second,
	}

	serverErr := make(chan error, 1)
	go func() {
		slog.Info("API listening.", slog.String("Addr", cfg.Server.Addr))
		if err := server.ListenAndServe(); !errors.Is(err, http.ErrServerClosed) {
			serverErr <- err
		}
	}()

	select {
	case err := <-serverErr:
		return err
	case <-ctx.Done():
	}

	// Graceful shutdown: önce drenaj, sonra 15 saniyelik kapanış bütçesi.
	slog.Info("Shutdown signal received; draining.")
	health.StartDraining()
	shutdownCtx, cancel := context.WithTimeout(context.Background(), 15*time.Second)
	defer cancel()
	return server.Shutdown(shutdownCtx)
}

// buildRouter, middleware zincirini ve uçları kurar. Modül rotaları ilgili
// fazlarda buraya eklenir (.NET Program.cs'teki Map*Endpoints sırası korunur).
func buildRouter(cfg config.Config, health *obs.Health, identityHandlers identityapi.Handlers, catalogHandlers catalogapi.Handlers) http.Handler {
	r := chi.NewRouter()

	// Eşleşmeyen rotalar .NET gibi gövdesiz 404 döner (chi'nin varsayılan
	// "404 page not found" metni kablo formatını bozar).
	r.NotFound(func(w http.ResponseWriter, _ *http.Request) {
		w.WriteHeader(http.StatusNotFound)
	})

	// Sağlık ve metrik uçları middleware zincirinin dışındadır (loglanmaz, izlenmez).
	r.Method(http.MethodGet, "/healthz", health.LivenessHandler())
	r.Method(http.MethodGet, "/ready", health.ReadinessHandler())
	r.Method(http.MethodGet, cfg.Observability.MetricsPath, obs.MetricsHandler())

	// /media: kimlik doğrulamasız statik dosya sunumu (frontend <img> auth gönderemez;
	// yollar tahmin edilemez GUID'lerden oluşur — .NET'teki davranışın aynısı).
	fileServer := http.StripPrefix("/media/", http.FileServer(http.Dir(cfg.Media.StoragePath)))
	r.Handle("/media/*", fileServer)

	// API rotaları: panik kurtarma → istek loglama → trace kimliği.
	r.Group(func(api chi.Router) {
		api.Use(httpx.Recovery)
		api.Use(httpx.RequestLogging(cfg.Observability.ExcludePathsFromRequestLogging))
		api.Use(httpx.TraceID)

		// Modül uçları (.NET Program.cs kayıt sırası): identity anonim uçlarıyla
		// birlikte; diğer modüller fazlar ilerledikçe eklenir.
		authMiddleware := httpx.JWTAuth(cfg.Identity.Jwt.Secret)
		identityapi.Mount(api, identityHandlers, authMiddleware)
		catalogapi.Mount(api, catalogHandlers, authMiddleware)
	})

	// Gelen istekler için OTel enstrümantasyonu; sağlık/metrik/medya yolları hariç.
	if cfg.Observability.Enabled && cfg.Observability.Tracing.Enabled {
		excluded := cfg.Observability.ExcludePathsFromRequestLogging
		return otelhttp.NewHandler(r, "pimly-api", otelhttp.WithFilter(func(req *http.Request) bool {
			for _, prefix := range excluded {
				if req.URL.Path == prefix || (len(req.URL.Path) > len(prefix) && req.URL.Path[:len(prefix)+1] == prefix+"/") {
					return false
				}
			}
			return true
		}))
	}
	return r
}

// mediaStorageCheck, medya dizinine deneme dosyası yazıp silerek depolamanın
// yazılabilir olduğunu doğrular (.NET MediaStorageHealthCheck karşılığı).
func mediaStorageCheck(storagePath string) func(ctx context.Context) error {
	return func(context.Context) error {
		probe := filepath.Join(storagePath, ".health-probe")
		if err := os.WriteFile(probe, []byte("ok"), 0o644); err != nil {
			return fmt.Errorf("medya depolama yazılamıyor: %w", err)
		}
		return os.Remove(probe)
	}
}
