package listingsync

import (
	"context"
	"log/slog"
	"strings"
	"sync"
	"time"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/channels/domain"
)

// Runner, keşif + senkron turunu bir araya getirir
// (.NET ListingSyncBackgroundService.RunOnceAsync portu). İki fazlı desen:
// önce tenant bağlamı olmadan bekleyen (tenant, pazaryeri) çiftleri keşfedilir
// (kirli satırlar tenant'lar arası taranır), sonra her çift için teklif
// (ucuz, onaysız) önce, içerik (pahalı, yeniden onaya girer) sonra senkronlanır.
type Runner struct {
	store         Store
	offerSyncer   *OfferSyncer
	contentSyncer *ContentSyncer
	now           func() time.Time

	// concurrency, aynı anda işlenecek kapsam sayısıdır. Kapsamlar arası
	// paralellik bedavadır: pazaryeri hız sınırları satıcı başına ayrıdır,
	// yani farklı tenant'lar birbirinin bütçesini tüketmez. 1 = sıralı.
	concurrency int

	// locker, aynı kapsamın iki worker tarafından eşzamanlı işlenmesini
	// engeller. nil ise kilitleme yapılmaz (tek örnekli çalıştırma, testler).
	locker ScopeLocker

	// marketplaces, bu worker örneğinin işleyeceği kanal kodlarıdır. Worker'lar
	// kanal başına çalıştığı için her örnek yalnızca kendi kanalını almalıdır;
	// boşsa tüm kanallar işlenir (tek worker'lı kurulum).
	marketplaces map[string]struct{}
}

// ScopeLocker, bir kapsamı (tenant, pazaryeri) tek worker'a ayırır. Kilit
// olmadan ikinci bir worker örneği aynı kirli satırları okur ve pazaryerine
// ÇİFT gönderim yapar — yani yatay ölçekleme ancak bununla güvenlidir.
type ScopeLocker interface {
	// TryLockScope, kapsamı kilitlemeyi dener. Kilit başkasındaysa
	// (nil, false, nil) döner; alınırsa release çağrılana kadar tutulur.
	TryLockScope(ctx context.Context, scope domain.ListingSyncScope) (release func(), locked bool, err error)
}

// NewRunner, bağımlılıklarıyla runner'ı oluşturur (sıralı, kilitsiz).
func NewRunner(store Store, offerSyncer *OfferSyncer, contentSyncer *ContentSyncer) *Runner {
	return &Runner{store: store, offerSyncer: offerSyncer, contentSyncer: contentSyncer,
		now: func() time.Time { return time.Now().UTC() }, concurrency: 1}
}

// WithConcurrency, aynı anda işlenecek kapsam sayısını belirler; 1'in altındaki
// değerler 1'e çekilir.
func (r *Runner) WithConcurrency(n int) *Runner {
	if n < 1 {
		n = 1
	}
	r.concurrency = n
	return r
}

// WithScopeLocker, kapsam kilidini etkinleştirir; birden fazla worker örneği
// çalıştırılacaksa zorunludur.
func (r *Runner) WithScopeLocker(locker ScopeLocker) *Runner {
	r.locker = locker
	return r
}

// WithMarketplaces, worker'ı belirtilen kanallarla sınırlar. Boş çağrı filtreyi
// kaldırır (tüm kanallar).
func (r *Runner) WithMarketplaces(codes ...string) *Runner {
	if len(codes) == 0 {
		r.marketplaces = nil
		return r
	}
	filter := make(map[string]struct{}, len(codes))
	for _, code := range codes {
		if trimmed := strings.TrimSpace(code); trimmed != "" {
			filter[trimmed] = struct{}{}
		}
	}
	r.marketplaces = filter
	return r
}

// handles, kapsamın bu worker'a ait olup olmadığını söyler.
func (r *Runner) handles(scope domain.ListingSyncScope) bool {
	if len(r.marketplaces) == 0 {
		return true
	}
	_, ok := r.marketplaces[scope.MarketplaceCode]
	return ok
}

// RunOnce, tek keşif+senkron turunu yürütür; iş yapılıp yapılmadığını döner
// (worker.RunLoop'un beklediği imza — dolu tur sonrası hemen tekrar denenir).
func (r *Runner) RunOnce(ctx context.Context, tenantFilter []uuid.UUID) (bool, error) {
	discovered, err := r.store.ListDirtyScopes(ctx, tenantFilter, r.now())
	if err != nil {
		return false, err
	}
	scopes := make([]domain.ListingSyncScope, 0, len(discovered))
	for _, scope := range discovered {
		if r.handles(scope) {
			scopes = append(scopes, scope)
		}
	}
	if len(scopes) == 0 {
		return false, nil
	}

	workers := r.concurrency
	if workers > len(scopes) {
		workers = len(scopes)
	}
	if workers <= 1 {
		for _, scope := range scopes {
			if ctx.Err() != nil {
				return true, ctx.Err()
			}
			r.runLockedScope(ctx, scope)
		}
		return true, nil
	}

	// Kapsamlar birbirinden bağımsızdır; sınırlı bir havuzla paralel işlenir.
	// Sıralı işleyiş 1000 mağazada tur başına yarım saate çıkardı.
	queue := make(chan domain.ListingSyncScope)
	var wg sync.WaitGroup
	for i := 0; i < workers; i++ {
		wg.Add(1)
		go func() {
			defer wg.Done()
			for scope := range queue {
				if ctx.Err() != nil {
					return
				}
				r.runLockedScope(ctx, scope)
			}
		}()
	}
	for _, scope := range scopes {
		if ctx.Err() != nil {
			break
		}
		queue <- scope
	}
	close(queue)
	wg.Wait()
	if ctx.Err() != nil {
		return true, ctx.Err()
	}
	return true, nil
}

// runLockedScope, kapsamı kilitleyip işler. Kilit başkasındaysa kapsam sessizce
// atlanır — başka bir worker onu zaten işliyor demektir, bir sonraki turda
// yeniden denenir.
//
// Panic kapsam düzeyinde yakalanır: tek bir mağazanın bozuk verisi, aynı
// havuzdaki diğer mağazaların senkronunu ve worker sürecini düşürmemelidir.
func (r *Runner) runLockedScope(ctx context.Context, scope domain.ListingSyncScope) {
	defer func() {
		if rec := recover(); rec != nil {
			slog.Error("Kapsam işlenirken panic; diğer kapsamlar etkilenmedi.",
				slog.String("TenantId", scope.TenantID.String()),
				slog.String("Marketplace", scope.MarketplaceCode),
				slog.Any("Panic", rec))
		}
	}()

	if r.locker == nil {
		r.runScope(ctx, scope)
		return
	}
	release, locked, err := r.locker.TryLockScope(ctx, scope)
	if err != nil {
		slog.Warn("Kapsam kilidi alınamadı.",
			slog.String("TenantId", scope.TenantID.String()),
			slog.String("Marketplace", scope.MarketplaceCode),
			slog.String("Error", err.Error()))
		return
	}
	if !locked {
		return
	}
	defer release()
	r.runScope(ctx, scope)
}

// runScope, tek (tenant, pazaryeri) çifti için teklif+içerik turunu yürütür;
// hatalar loglanır, döngü durmaz (diğer kapsamlarla devam edilir).
func (r *Runner) runScope(ctx context.Context, scope domain.ListingSyncScope) {
	offerResult := r.offerSyncer.Sync(ctx, scope.TenantID, scope.MarketplaceCode)
	if offerResult.IsFailure() {
		slog.Warn("Teklif senkronu başarısız.",
			slog.String("TenantId", scope.TenantID.String()),
			slog.String("Marketplace", scope.MarketplaceCode),
			slog.String("Error", offerResult.Err().Message))
	} else if summary := offerResult.Value(); summary.Pushed > 0 || summary.Failed > 0 {
		slog.Info("Teklif senkronu tamamlandı.",
			slog.String("TenantId", scope.TenantID.String()),
			slog.String("Marketplace", scope.MarketplaceCode),
			slog.Int("Examined", summary.Examined), slog.Int("Skipped", summary.Skipped),
			slog.Int("Pushed", summary.Pushed), slog.Int("Failed", summary.Failed))
	}

	contentResult := r.contentSyncer.Sync(ctx, scope.TenantID, scope.MarketplaceCode)
	if contentResult.IsFailure() {
		slog.Warn("İçerik senkronu başarısız.",
			slog.String("TenantId", scope.TenantID.String()),
			slog.String("Marketplace", scope.MarketplaceCode),
			slog.String("Error", contentResult.Err().Message))
	} else if summary := contentResult.Value(); summary.Created > 0 || summary.Updated > 0 || summary.Failed > 0 {
		slog.Info("İçerik senkronu tamamlandı.",
			slog.String("TenantId", scope.TenantID.String()),
			slog.String("Marketplace", scope.MarketplaceCode),
			slog.Int("Examined", summary.Examined), slog.Int("Skipped", summary.Skipped),
			slog.Int("Created", summary.Created), slog.Int("Updated", summary.Updated),
			slog.Int("Failed", summary.Failed))
	}
}
