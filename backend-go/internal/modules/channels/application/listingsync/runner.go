package listingsync

import (
	"context"
	"log/slog"
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
}

// NewRunner, bağımlılıklarıyla runner'ı oluşturur.
func NewRunner(store Store, offerSyncer *OfferSyncer, contentSyncer *ContentSyncer) *Runner {
	return &Runner{store: store, offerSyncer: offerSyncer, contentSyncer: contentSyncer, now: func() time.Time { return time.Now().UTC() }}
}

// RunOnce, tek keşif+senkron turunu yürütür; iş yapılıp yapılmadığını döner
// (worker.RunLoop'un beklediği imza — dolu tur sonrası hemen tekrar denenir).
func (r *Runner) RunOnce(ctx context.Context, tenantFilter []uuid.UUID) (bool, error) {
	scopes, err := r.store.ListDirtyScopes(ctx, tenantFilter, r.now())
	if err != nil {
		return false, err
	}
	if len(scopes) == 0 {
		return false, nil
	}

	for _, scope := range scopes {
		if ctx.Err() != nil {
			return true, ctx.Err()
		}
		r.runScope(ctx, scope)
	}
	return true, nil
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
