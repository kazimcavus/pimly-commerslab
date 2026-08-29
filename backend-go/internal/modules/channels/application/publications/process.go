// Package publications, tenant'ın satılabilir kalemlerini bir pazaryerine
// ilk kez göndeme (yayın/publish) işini yürütür (.NET
// ProcessPublicationHandler karşılığı). ProductImportRun'ın "outbound
// aynası"dır: worker kuyruğu channels.product_publication_runs tablosu
// üzerinden FOR UPDATE SKIP LOCKED ile beslenir.
//
// Neden iki adım: yeni ürün gönderimi ile içerik güncellemesi aynı payload ve
// aynı pazaryeri ucunu kullanır. Run yalnızca KAYDI AÇAR (eşlenmiş
// kategorilerdeki henüz listelenmemiş kalemler için pending+dirty listeleme
// satırı); teslimatı tek bir yol (listingsync.ContentSyncer) üstlenir, böylece
// delta/hash ve backoff mantığı tek yerde kalır — publications worker'ı
// Trendyol'a DOĞRUDAN yazmaz, listing-sync'in bir sonraki turunu tetikler.
//
// Kapsam: kategorisi eşlenmemiş kalemler hiç kaydedilmez — pazaryerinde
// nereye listeleneceği bilinmediği için gönderilemezler.
package publications

import (
	"context"
	"log/slog"
	"time"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/channels/application/listingsync"
	"pimly.commerslab/backend-go/internal/modules/channels/domain"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// Store, işlemcinin ihtiyaç duyduğu Channels kalıcılık yüzeyidir; somut
// karşılığı channels/infrastructure.Repository'dir.
type Store interface {
	// UpdatePublicationRun, iş kaydını (yeni hata satırlarıyla) kalıcılaştırır.
	UpdatePublicationRun(ctx context.Context, run *domain.ProductPublicationRun) error

	// GetConnection, tenant'ın pazaryeri bağlantısını döner; yoksa nil.
	GetConnection(ctx context.Context, tenantID uuid.UUID, marketplaceCode string) (*domain.MarketplaceConnection, error)

	// ListMappedCategoryIDs, tenant'ın bu pazaryerinde eşlediği tüm catalog
	// kategori kimliklerini döner.
	ListMappedCategoryIDs(ctx context.Context, tenantID uuid.UUID, marketplaceCode string) ([]uuid.UUID, error)
}

// CategoryItemsGateway, kategorilerdeki satılabilir kalem kimliklerini okuyan porttur.
type CategoryItemsGateway interface {
	ListItemIDsByCategories(ctx context.Context, tenantID uuid.UUID, categoryIDs []uuid.UUID) ([]uuid.UUID, error)
}

// ListingStore, listeleme tohumlama için gereken kalıcılık yüzeyidir; somut
// karşılığı channels/infrastructure.ListingRepository'dir.
type ListingStore interface {
	// ListByProductItems, kalemlerin bu pazaryerindeki mevcut listelemelerini döner.
	ListByProductItems(ctx context.Context, tenantID uuid.UUID, marketplaceCode string, productItemIDs []uuid.UUID) ([]*domain.ProductListing, error)

	// AddRange, yeni listeleme kayıtlarını ekler.
	AddRange(ctx context.Context, listings []*domain.ProductListing) error
}

// ContentSyncer, teslimatın tek yolu olan içerik senkron akışıdır; somut
// karşılığı listingsync.ContentSyncer'dır.
type ContentSyncer interface {
	Sync(ctx context.Context, tenantID uuid.UUID, marketplaceCode string) sharedkernel.ResultOf[listingsync.ContentSyncSummary]
}

// Processor, claim edilmiş yayın işlerini yürüten orkestratördür.
type Processor struct {
	store       Store
	items       CategoryItemsGateway
	listings    ListingStore
	contentSync ContentSyncer
	now         func() time.Time
}

// NewProcessor, bağımlılıklarıyla işlemciyi oluşturur.
func NewProcessor(store Store, items CategoryItemsGateway, listings ListingStore, contentSync ContentSyncer) *Processor {
	return &Processor{store: store, items: items, listings: listings, contentSync: contentSync, now: func() time.Time { return time.Now().UTC() }}
}

// enrollmentResult, kayıt açma adımının özetidir.
type enrollmentResult struct {
	total   int
	created int
}

// Process, running durumundaki yayın işini uçtan uca yürütür.
func (p *Processor) Process(ctx context.Context, run *domain.ProductPublicationRun) error {
	if run.Status != domain.PublicationRunning {
		return sharedkernel.NewFailureError("publications: iş running durumunda değil: " + string(run.Status))
	}

	connection, err := p.store.GetConnection(ctx, run.TenantID, run.MarketplaceCode)
	if err != nil {
		return err
	}
	if connection == nil || !connection.IsEnabled {
		return p.failRun(ctx, run, "Marketplace connection is missing or disabled.")
	}

	enrolled, err := p.enrollListings(ctx, run)
	if err != nil {
		return err
	}
	total := enrolled.total
	run.UpdateProgress(0, 0, 0, &total)
	if err := p.store.UpdatePublicationRun(ctx, run); err != nil {
		return err
	}

	// Teslimat tek yoldan: içerik senkronu kirli (yeni açılan dahil)
	// listelemeleri gönderir.
	syncResult := p.contentSync.Sync(ctx, run.TenantID, run.MarketplaceCode)
	if syncResult.IsFailure() {
		return p.failRun(ctx, run, syncResult.Err().Message)
	}
	summary := syncResult.Value()
	published := summary.Created + summary.Updated
	run.UpdateProgress(summary.Examined, published, summary.Failed, &total)

	if completeResult := run.MarkCompleted(p.now()); completeResult.IsFailure() {
		return sharedkernel.NewFailureError("publications: iş tamamlanamadı: " + completeResult.Err().Message)
	}
	if err := p.store.UpdatePublicationRun(ctx, run); err != nil {
		return err
	}
	slog.Info("Publication finished.",
		slog.String("RunId", run.ID.String()), slog.String("TenantId", run.TenantID.String()),
		slog.Int("Enrolled", enrolled.created), slog.Int("Created", summary.Created),
		slog.Int("Updated", summary.Updated), slog.Int("Failed", summary.Failed))
	return nil
}

// failRun, işi altyapı hatasıyla sonlandırır ve kaydeder.
func (p *Processor) failRun(ctx context.Context, run *domain.ProductPublicationRun, message string) error {
	slog.Error("Publication failed.",
		slog.String("RunId", run.ID.String()), slog.String("TenantId", run.TenantID.String()),
		slog.String("Error", message))
	if failResult := run.MarkFailed(p.now(), message); failResult.IsFailure() {
		return nil
	}
	return p.store.UpdatePublicationRun(ctx, run)
}

// enrollListings, eşlenmiş kategorilerdeki kalemler için eksik listeleme
// kayıtlarını açar. Var olan kayıtlara dokunulmaz; yeni kayıtlar baştan kirli
// olduğu için bir sonraki içerik senkron turunda gönderilirler
// (.NET EnrollListingsAsync portu).
func (p *Processor) enrollListings(ctx context.Context, run *domain.ProductPublicationRun) (enrollmentResult, error) {
	categoryIDs, err := p.store.ListMappedCategoryIDs(ctx, run.TenantID, run.MarketplaceCode)
	if err != nil {
		return enrollmentResult{}, err
	}
	if len(categoryIDs) == 0 {
		run.AddError(uuid.Nil, "Bu pazaryeri için eşlenmiş kategori yok; yayınlanacak kalem bulunamadı.")
		return enrollmentResult{}, nil
	}

	itemIDs, err := p.items.ListItemIDsByCategories(ctx, run.TenantID, categoryIDs)
	if err != nil {
		return enrollmentResult{}, err
	}
	if len(itemIDs) == 0 {
		return enrollmentResult{}, nil
	}

	existing, err := p.listings.ListByProductItems(ctx, run.TenantID, run.MarketplaceCode, itemIDs)
	if err != nil {
		return enrollmentResult{}, err
	}
	alreadyListed := make(map[uuid.UUID]struct{}, len(existing))
	for _, listing := range existing {
		alreadyListed[listing.ProductItemID] = struct{}{}
	}

	now := p.now()
	created := []*domain.ProductListing{}
	for _, itemID := range itemIDs {
		if _, listed := alreadyListed[itemID]; listed {
			continue
		}
		createResult := domain.NewListing(run.TenantID, run.MarketplaceCode, itemID, now)
		if createResult.IsFailure() {
			run.AddError(itemID, createResult.Err().Message)
			continue
		}
		created = append(created, createResult.Value())
	}
	if len(created) > 0 {
		if err := p.listings.AddRange(ctx, created); err != nil {
			return enrollmentResult{}, err
		}
	}
	return enrollmentResult{total: len(itemIDs), created: len(created)}, nil
}
