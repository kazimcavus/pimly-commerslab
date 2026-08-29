// pimly-taxonomy-sync-worker, pazaryeri kategori ağacı senkron işlerini işler
// (.NET Pimly.TaxonomySync.Worker karşılığı). İki görev yürütür:
//
//  1. Kuyruk işleyici: channels.taxonomy_sync_runs tablosundaki pending işleri
//     FOR UPDATE SKIP LOCKED ile claim eder, Trendyol'dan tüm ağacı çekip
//     düzleştirir ve 250'lik partilerle cache'e upsert eder; ilerleme her
//     partide kaydedilir.
//  2. Zamanlayıcı: TimesUtc slotlarını denetler; slot başladıysa ve o slotta
//     iş oluşturulmamışsa yeni senkron kuyruklar.
package main

import (
	"context"
	"fmt"
	"log/slog"
	"os"
	"sort"
	"strings"
	"time"

	"pimly.commerslab/backend-go/internal/integration/trendyol"
	channelsapp "pimly.commerslab/backend-go/internal/modules/channels/application"
	channelsinfra "pimly.commerslab/backend-go/internal/modules/channels/infrastructure"
	"pimly.commerslab/backend-go/internal/modules/channels/domain"
	"pimly.commerslab/backend-go/internal/platform/config"
	"pimly.commerslab/backend-go/internal/platform/obs"
	"pimly.commerslab/backend-go/internal/platform/pg"
	"pimly.commerslab/backend-go/internal/platform/worker"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// upsertBatchSize, kategori upsert partilerinin boyutudur (.NET UpsertBatchSize).
const upsertBatchSize = 250

func main() {
	if err := run(); err != nil {
		slog.Error("Taxonomy sync worker başlatılamadı.", slog.Any("Error", err))
		os.Exit(1)
	}
}

// run, worker yaşam döngüsünü yönetir.
func run() error {
	ctx, stop := worker.Setup("pimly-taxonomy-sync-worker")
	defer stop()

	cfg, err := config.Load("pimly-taxonomy-sync-worker")
	if err != nil {
		return err
	}
	if cfg.Server.Addr == ":7000" {
		cfg.Server.Addr = ":7002"
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

	var taxonomyClient trendyol.TaxonomyClient = trendyol.StubTaxonomyClient{}
	if !cfg.Channels.UseStubTaxonomyClient {
		taxonomyClient = trendyol.NewTaxonomyClient(
			trendyol.NewClient(cfg.Channels.TrendyolApiBaseUrl, trendyol.DefaultRateLimits()))
	}

	processor := &syncProcessor{repo: repo, client: taxonomyClient}

	// Zamanlayıcı ayrı goroutine'de koşar; ana döngü kuyruğu işler.
	if cfg.Channels.TaxonomySyncSchedule.Enabled {
		scheduler := &scheduler{repo: repo, schedule: cfg.Channels.TaxonomySyncSchedule}
		checkInterval := time.Duration(maxInt(15, cfg.Channels.TaxonomySyncSchedule.CheckIntervalSeconds)) * time.Second
		go worker.RunLoop(ctx, "taxonomy-scheduler", checkInterval, scheduler.tick)
	} else {
		slog.Info("Scheduled taxonomy sync is disabled.")
	}

	pollInterval := time.Duration(maxInt(1, cfg.Channels.WorkerPollIntervalSeconds)) * time.Second
	worker.RunLoop(ctx, "taxonomy-sync", pollInterval, processor.iterate)
	return nil
}

// maxInt, iki tamsayının büyüğünü döner.
func maxInt(a, b int) int {
	if a > b {
		return a
	}
	return b
}

// syncProcessor, kuyruktaki taksonomi işlerini yürütür
// (.NET ProcessTaxonomySyncHandler portu).
type syncProcessor struct {
	repo   *channelsinfra.Repository
	client trendyol.TaxonomyClient
}

// iterate, sıradaki pending işi claim edip işler; iş yoksa false döner.
func (p *syncProcessor) iterate(ctx context.Context) (bool, error) {
	run, err := p.repo.ClaimNextPendingTaxonomyRun(ctx)
	if err != nil {
		return false, err
	}
	if run == nil {
		return false, nil
	}

	credentials, err := p.resolveCredentials(ctx, run.MarketplaceCode)
	if err != nil {
		return true, p.failRun(ctx, run, err.Error())
	}

	fetchResult := p.client.FetchAllCategories(ctx, credentials)
	if fetchResult.IsFailure() {
		return true, p.failRun(ctx, run, fetchResult.Err().Message)
	}
	categories := fetchResult.Value()
	syncedAt := time.Now().UTC()

	total := len(categories)
	run.ProcessedCount = 0
	run.TotalEstimate = &total
	if err := p.repo.UpdateTaxonomyRun(ctx, run); err != nil {
		return true, err
	}

	processed := 0
	for start := 0; start < len(categories); start += upsertBatchSize {
		if ctx.Err() != nil {
			return true, ctx.Err()
		}
		end := start + upsertBatchSize
		if end > len(categories) {
			end = len(categories)
		}
		if err := p.repo.UpsertExternalCategoriesBatch(ctx, run.MarketplaceCode, categories[start:end], syncedAt); err != nil {
			return true, p.failRun(ctx, run, err.Error())
		}
		processed = end
		run.ProcessedCount = processed
		if err := p.repo.UpdateTaxonomyRun(ctx, run); err != nil {
			return true, err
		}
	}

	if completeResult := run.MarkCompleted(time.Now().UTC(), processed); completeResult.IsFailure() {
		return true, fmt.Errorf("taksonomi işi tamamlanamadı: %s", completeResult.Err().Message)
	}
	if err := p.repo.UpdateTaxonomyRun(ctx, run); err != nil {
		return true, err
	}
	slog.Info("Taxonomy sync completed.",
		slog.String("SyncRunId", run.ID.String()),
		slog.String("Marketplace", run.MarketplaceCode),
		slog.Int("CategoryCount", processed))
	return true, nil
}

// resolveCredentials, pazaryeri için etkin herhangi bir bağlantının kimlik
// bilgilerini döner (taksonomi pazaryeri-globaldir; tenant seçimi önemsizdir).
func (p *syncProcessor) resolveCredentials(ctx context.Context, marketplaceCode string) (*channelsapp.MarketplaceCredentials, error) {
	connection, err := p.repo.GetAnyEnabledConnection(ctx, marketplaceCode)
	if err != nil {
		return nil, err
	}
	if connection == nil {
		return nil, nil
	}
	return &channelsapp.MarketplaceCredentials{
		SellerID: connection.SellerID, ApiKey: connection.ApiKey, ApiSecret: connection.ApiSecret}, nil
}

// failRun, işi hata durumuyla sonlandırır.
func (p *syncProcessor) failRun(ctx context.Context, run *domain.TaxonomySyncRun, message string) error {
	if failResult := run.MarkFailed(time.Now().UTC(), message); failResult.IsFailure() {
		return nil
	}
	slog.Error("Taxonomy sync failed.",
		slog.String("SyncRunId", run.ID.String()),
		slog.String("Marketplace", run.MarketplaceCode),
		slog.String("Error", message))
	return p.repo.UpdateTaxonomyRun(ctx, run)
}

// scheduler, TimesUtc slotlarını denetleyip gerektiğinde senkron kuyruklar
// (.NET RunScheduledTaxonomySyncHandler portu).
type scheduler struct {
	repo     *channelsinfra.Repository
	schedule config.TaxonomySyncScheduleConfig
}

// tick, tek zamanlayıcı denetimidir.
func (s *scheduler) tick(ctx context.Context) (bool, error) {
	slotStart, err := currentSlotStart(time.Now().UTC(), s.schedule.TimesUtc)
	if err != nil {
		return false, err
	}

	code := sharedkernel.MarketplaceCodeTrendyol
	hasRun, err := s.repo.HasTaxonomyRunSince(ctx, code, slotStart)
	if err != nil {
		return false, err
	}
	if hasRun {
		return false, nil
	}

	active, err := s.repo.GetActiveTaxonomyRun(ctx, code)
	if err != nil {
		return false, err
	}
	if active != nil {
		return false, nil
	}

	run := domain.NewTaxonomySyncRun(code, time.Now().UTC())
	if err := s.repo.AddTaxonomyRun(ctx, run); err != nil {
		return false, err
	}
	slog.Info("Scheduled taxonomy sync enqueued.",
		slog.String("Marketplace", code),
		slog.String("SyncRunId", run.ID.String()),
		slog.Time("SlotStartUtc", slotStart))
	return false, nil
}

// currentSlotStart, şu ana göre en son başlamış slotun UTC başlangıcını döner:
// bugünün geçmiş saatlerinden en büyüğü, yoksa dünün son saati.
func currentSlotStart(now time.Time, timesUtc []string) (time.Time, error) {
	if len(timesUtc) == 0 {
		return time.Time{}, fmt.Errorf("taksonomi zamanlaması boş")
	}
	type hm struct{ hour, minute int }
	slots := make([]hm, 0, len(timesUtc))
	for _, raw := range timesUtc {
		parts := strings.Split(strings.TrimSpace(raw), ":")
		if len(parts) != 2 {
			return time.Time{}, fmt.Errorf("geçersiz zamanlama saati: %q", raw)
		}
		var slot hm
		if _, err := fmt.Sscanf(parts[0]+" "+parts[1], "%d %d", &slot.hour, &slot.minute); err != nil {
			return time.Time{}, fmt.Errorf("geçersiz zamanlama saati: %q", raw)
		}
		slots = append(slots, slot)
	}
	sort.Slice(slots, func(i, j int) bool {
		return slots[i].hour*60+slots[i].minute < slots[j].hour*60+slots[j].minute
	})

	today := time.Date(now.Year(), now.Month(), now.Day(), 0, 0, 0, 0, time.UTC)
	var latest *time.Time
	for _, slot := range slots {
		candidate := today.Add(time.Duration(slot.hour)*time.Hour + time.Duration(slot.minute)*time.Minute)
		if !candidate.After(now) {
			value := candidate
			latest = &value
		}
	}
	if latest != nil {
		return *latest, nil
	}
	// Bugün hiçbir slot başlamadıysa dünün son slotu geçerlidir.
	last := slots[len(slots)-1]
	return today.AddDate(0, 0, -1).Add(
		time.Duration(last.hour)*time.Hour + time.Duration(last.minute)*time.Minute), nil
}
