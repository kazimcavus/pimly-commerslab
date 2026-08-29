package listingsync

import (
	"context"
	"log/slog"
	"time"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/channels/application"
	"pimly.commerslab/backend-go/internal/modules/channels/domain"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// contentPageSize, tek turda incelenecek azami kirli listeleme sayısıdır
// (.NET SyncListingContentHandler.PageSize).
const contentPageSize = 200

// contentBaseBackoff/contentMaxBackoff, içerik senkron hatalarında bekleme
// süreleridir (.NET SyncListingContentHandler sabitleri).
const (
	contentBaseBackoff = 60 * time.Second
	contentMaxBackoff  = time.Hour
)

// ContentSyncer, bir pazaryerindeki "içerik kirli" listelemeleri toplu olarak
// pazaryerine gönderir (.NET SyncListingContentHandler portu). Hiç
// gönderilmemiş (pending) listelemeler yeni kart olarak, canlı olanlar
// güncelleme olarak gider.
//
// Delta: payload hash'i saklananla aynıysa çağrı yapılmaz — içerik gönderimi
// ürünü yeniden onaya soktuğu için gereksiz gönderim gerçek zarar verir.
// Ön koşul eksikliği (kategori eşlemesi/fiyat yok) atlanır ve kirlilik
// korunur; taşıma hatası backoff kurar.
type ContentSyncer struct {
	store     Store
	sources   CatalogListingSourceGateway
	prices    PricingChannelPriceGateway
	stocks    InventoryStockGateway
	assembler *Assembler
	resolver  *ClientResolver
	now       func() time.Time
}

// NewContentSyncer, bağımlılıklarıyla içerik senkronizatörünü oluşturur.
func NewContentSyncer(
	store Store, sources CatalogListingSourceGateway, prices PricingChannelPriceGateway,
	stocks InventoryStockGateway, assembler *Assembler, resolver *ClientResolver,
) *ContentSyncer {
	return &ContentSyncer{
		store: store, sources: sources, prices: prices, stocks: stocks,
		assembler: assembler, resolver: resolver, now: func() time.Time { return time.Now().UTC() },
	}
}

// preparedListing, gönderime hazırlanmış tek listelemedir.
type preparedListing struct {
	listing  *domain.ProductListing
	request  MarketplaceListingRequest
	hash     string
	isUpdate bool
}

// Sync, verilen tenant+pazaryeri kapsamı için tek içerik senkron turu yürütür.
func (s *ContentSyncer) Sync(ctx context.Context, tenantID uuid.UUID, marketplaceCode string) sharedkernel.ResultOf[ContentSyncSummary] {
	now := s.now()
	dirty, err := s.store.ListDirty(ctx, tenantID, marketplaceCode, now, contentPageSize)
	if err != nil {
		return sharedkernel.FailOf[ContentSyncSummary](sharedkernel.NewInternalError(err.Error()))
	}
	candidates := make([]*domain.ProductListing, 0, len(dirty))
	for _, listing := range dirty {
		if listing.ContentDirtyAt != nil {
			candidates = append(candidates, listing)
		}
	}
	if len(candidates) == 0 {
		return sharedkernel.OkOf(ContentSyncSummary{})
	}

	connection, err := s.store.GetConnection(ctx, tenantID, marketplaceCode)
	if err != nil {
		return sharedkernel.FailOf[ContentSyncSummary](sharedkernel.NewInternalError(err.Error()))
	}
	if connection == nil || !connection.IsEnabled {
		return sharedkernel.FailOf[ContentSyncSummary](sharedkernel.NewValidationError(
			"Marketplace connection is missing or disabled."))
	}
	clientResult := s.resolver.ResolveListing(marketplaceCode)
	if clientResult.IsFailure() {
		return sharedkernel.FailOf[ContentSyncSummary](clientResult.Err())
	}
	client := clientResult.Value()
	credentials := &application.MarketplaceCredentials{
		SellerID: connection.SellerID, ApiKey: connection.ApiKey, ApiSecret: connection.ApiSecret}

	itemIDs := make([]uuid.UUID, len(candidates))
	for i, listing := range candidates {
		itemIDs[i] = listing.ProductItemID
	}
	sourceRows, err := s.sources.Get(ctx, tenantID, itemIDs)
	if err != nil {
		return sharedkernel.FailOf[ContentSyncSummary](sharedkernel.NewInternalError(err.Error()))
	}
	sourceByItem := make(map[uuid.UUID]application.CatalogListingSource, len(sourceRows))
	for _, source := range sourceRows {
		sourceByItem[source.ProductItemID] = source
	}

	priceRows, err := s.prices.ListForMarketplace(ctx, tenantID, marketplaceCode)
	if err != nil {
		return sharedkernel.FailOf[ContentSyncSummary](sharedkernel.NewInternalError(err.Error()))
	}
	priceByItem := make(map[uuid.UUID]DecidedChannelPrice, len(priceRows))
	for _, price := range priceRows {
		priceByItem[price.ProductItemID] = price
	}

	quantityByItem, err := s.stocks.GetQuantities(ctx, tenantID, itemIDs)
	if err != nil {
		return sharedkernel.FailOf[ContentSyncSummary](sharedkernel.NewInternalError(err.Error()))
	}

	pending := []preparedListing{}
	skipped := 0
	for _, listing := range candidates {
		prepared, err := s.prepare(ctx, tenantID, marketplaceCode, listing, sourceByItem, priceByItem, quantityByItem, now)
		if err != nil {
			return sharedkernel.FailOf[ContentSyncSummary](sharedkernel.NewInternalError(err.Error()))
		}
		if prepared == nil {
			skipped++
			continue
		}
		pending = append(pending, *prepared)
	}

	created, updated, failed := 0, 0, 0
	// Yeni kart ile güncelleme farklı uçlara gider; bu yüzden ayrı gruplanır.
	for _, isUpdate := range []bool{false, true} {
		group := filterByUpdate(pending, isUpdate)
		for _, batch := range chunkOffers(group, client.MaxBatchSize()) {
			requests := make([]MarketplaceListingRequest, len(batch))
			for i, entry := range batch {
				requests[i] = entry.request
			}
			result := client.Submit(ctx, credentials, requests, isUpdate)
			for _, entry := range batch {
				if result.IsSuccess() {
					markResult := entry.listing.MarkContentSubmitted(entry.hash, result.Value().SubmissionReference, now)
					if markResult.IsFailure() {
						failed++
					} else if isUpdate {
						updated++
					} else {
						created++
					}
				} else {
					entry.listing.RegisterSyncFailure(nextContentAttempt(entry.listing.SyncAttempts, now))
					failed++
				}
				if err := s.store.Update(ctx, entry.listing); err != nil {
					return sharedkernel.FailOf[ContentSyncSummary](sharedkernel.NewInternalError(err.Error()))
				}
			}
			if result.IsFailure() {
				slog.Warn("İçerik gönderimi başarısız.",
					slog.String("Marketplace", marketplaceCode), slog.Int("Count", len(batch)),
					slog.Bool("IsUpdate", isUpdate), slog.String("Error", result.Err().Message))
			}
		}
	}

	return sharedkernel.OkOf(ContentSyncSummary{
		Examined: len(candidates), Skipped: skipped, Created: created, Updated: updated, Failed: failed})
}

// prepare, tek listelemeyi gönderime hazırlar; ön koşul eksikse ya da hash
// değişmemişse nil döner (.NET PrepareAsync portu).
func (s *ContentSyncer) prepare(
	ctx context.Context,
	tenantID uuid.UUID,
	marketplaceCode string,
	listing *domain.ProductListing,
	sourceByItem map[uuid.UUID]application.CatalogListingSource,
	priceByItem map[uuid.UUID]DecidedChannelPrice,
	quantityByItem map[uuid.UUID]int,
	now time.Time,
) (*preparedListing, error) {
	source, ok := sourceByItem[listing.ProductItemID]
	if !ok {
		return nil, nil
	}
	price, ok := priceByItem[listing.ProductItemID]
	if !ok {
		// Fiyatı kararlaştırılmamış kalem listelenemez; kirlilik korunur ki
		// fiyat girilince sonraki tur yakalasın.
		return nil, nil
	}
	quantity := quantityByItem[listing.ProductItemID]

	assembled := s.assembler.Assemble(ctx, tenantID, marketplaceCode, source, price, quantity)
	if assembled.IsFailure() {
		slog.Debug("Listeleme atlandı.",
			slog.String("ItemId", listing.ProductItemID.String()),
			slog.String("Reason", assembled.Err().Message))
		return nil, nil
	}
	request := assembled.Value()
	hash := ComputeContentHash(request)
	if !listing.NeedsContentSync(hash) {
		// İçerik aslında değişmemiş: bayrağı temizle, pazaryerini yeniden onaya sokma.
		if markResult := listing.MarkContentSubmitted(hash, listing.SubmissionReference, now); markResult.IsSuccess() {
			if err := s.store.Update(ctx, listing); err != nil {
				return nil, err
			}
		}
		return nil, nil
	}
	// Dış kimliği olan listeleme pazaryerinde zaten var → güncelleme ucu.
	return &preparedListing{listing: listing, request: request, hash: hash, isUpdate: listing.ExternalListingID != nil}, nil
}

// nextContentAttempt, içerik senkron hatası sonrası bir sonraki deneme
// zamanını hesaplar: 60s·2ⁿ, tavan 1 saat, n en fazla 6 (.NET NextAttemptAt portu).
func nextContentAttempt(attempts int, now time.Time) time.Time {
	return now.Add(backoffDelay(attempts, 6, contentBaseBackoff, contentMaxBackoff))
}

// filterByUpdate, hazırlanmış listelemeleri güncelleme/yeni-kart durumuna göre süzer.
func filterByUpdate(pending []preparedListing, isUpdate bool) []preparedListing {
	filtered := make([]preparedListing, 0, len(pending))
	for _, entry := range pending {
		if entry.isUpdate == isUpdate {
			filtered = append(filtered, entry)
		}
	}
	return filtered
}
