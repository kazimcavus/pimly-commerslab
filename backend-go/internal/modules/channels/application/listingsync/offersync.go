package listingsync

import (
	"context"
	"log/slog"
	"math"
	"time"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/channels/application"
	"pimly.commerslab/backend-go/internal/modules/channels/domain"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// offerPageSize, tek turda incelenecek azami kirli listeleme sayısıdır
// (.NET SyncListingOffersHandler.PageSize).
const offerPageSize = 1000

// offerBaseBackoff/offerMaxBackoff, teklif senkron hatalarında bekleme
// süreleridir (.NET SyncListingOffersHandler sabitleri).
const (
	offerBaseBackoff = 30 * time.Second
	offerMaxBackoff  = time.Hour
)

// OfferSyncer, bir pazaryerindeki "teklif kirli" listelemelerin fiyat/stok
// bilgisini toplu olarak pazaryerine gönderir (.NET SyncListingOffersHandler
// portu). Olay başına push yerine bu debounce edilmiş toplu tur kullanılır.
//
// Delta: her listeleme için güncel fiyat/stoktan hash hesaplanır; saklanan
// hash ile aynıysa pazaryerine çağrı yapılmaz. Taşıma hatası listelemenin
// durumunu değiştirmez, yalnız backoff kurar; kirlilik korunduğu için sonraki
// tur doğal olarak yeniden dener.
type OfferSyncer struct {
	store    Store
	prices   PricingChannelPriceGateway
	stocks   InventoryStockGateway
	resolver *ClientResolver
	now      func() time.Time
}

// NewOfferSyncer, bağımlılıklarıyla teklif senkronizatörünü oluşturur.
func NewOfferSyncer(store Store, prices PricingChannelPriceGateway, stocks InventoryStockGateway, resolver *ClientResolver) *OfferSyncer {
	return &OfferSyncer{store: store, prices: prices, stocks: stocks, resolver: resolver, now: func() time.Time { return time.Now().UTC() }}
}

// Sync, verilen tenant+pazaryeri kapsamı için tek teklif senkron turu yürütür.
func (s *OfferSyncer) Sync(ctx context.Context, tenantID uuid.UUID, marketplaceCode string) sharedkernel.ResultOf[OfferSyncSummary] {
	now := s.now()
	dirty, err := s.store.ListDirty(ctx, tenantID, marketplaceCode, now, offerPageSize)
	if err != nil {
		return sharedkernel.FailOf[OfferSyncSummary](sharedkernel.NewInternalError(err.Error()))
	}
	candidates := make([]*domain.ProductListing, 0, len(dirty))
	for _, listing := range dirty {
		if listing.OfferDirtyAt != nil {
			candidates = append(candidates, listing)
		}
	}
	if len(candidates) == 0 {
		return sharedkernel.OkOf(OfferSyncSummary{})
	}

	connection, err := s.store.GetConnection(ctx, tenantID, marketplaceCode)
	if err != nil {
		return sharedkernel.FailOf[OfferSyncSummary](sharedkernel.NewInternalError(err.Error()))
	}
	if connection == nil || !connection.IsEnabled {
		return sharedkernel.FailOf[OfferSyncSummary](sharedkernel.NewValidationError(
			"Marketplace connection is missing or disabled."))
	}
	clientResult := s.resolver.ResolveOffer(marketplaceCode)
	if clientResult.IsFailure() {
		return sharedkernel.FailOf[OfferSyncSummary](clientResult.Err())
	}
	client := clientResult.Value()
	credentials := &application.MarketplaceCredentials{
		SellerID: connection.SellerID, ApiKey: connection.ApiKey, ApiSecret: connection.ApiSecret}

	priceRows, err := s.prices.ListForMarketplace(ctx, tenantID, marketplaceCode)
	if err != nil {
		return sharedkernel.FailOf[OfferSyncSummary](sharedkernel.NewInternalError(err.Error()))
	}
	priceByItem := make(map[uuid.UUID]DecidedChannelPrice, len(priceRows))
	for _, price := range priceRows {
		priceByItem[price.ProductItemID] = price
	}

	itemIDs := make([]uuid.UUID, len(candidates))
	for i, listing := range candidates {
		itemIDs[i] = listing.ProductItemID
	}
	quantityByItem, err := s.stocks.GetQuantities(ctx, tenantID, itemIDs)
	if err != nil {
		return sharedkernel.FailOf[OfferSyncSummary](sharedkernel.NewInternalError(err.Error()))
	}

	type pendingOffer struct {
		listing *domain.ProductListing
		offer   MarketplaceOfferUpdate
		hash    string
	}
	pending := []pendingOffer{}
	skipped := 0
	for _, listing := range candidates {
		offer := buildOffer(listing, priceByItem, quantityByItem)
		if offer == nil {
			// Fiyatı henüz kararlaştırılmamış kalem gönderilemez; kirlilik
			// korunur ki fiyat girildiğinde sonraki tur yakalasın.
			skipped++
			continue
		}
		hash := ComputeOfferHash(*offer)
		if !listing.NeedsOfferSync(hash) {
			// Değer aslında değişmemiş: bayrağı temizle, pazaryerini hiç rahatsız etme.
			if markResult := listing.MarkOfferSynced(hash, now); markResult.IsFailure() {
				skipped++
				continue
			}
			if err := s.store.Update(ctx, listing); err != nil {
				return sharedkernel.FailOf[OfferSyncSummary](sharedkernel.NewInternalError(err.Error()))
			}
			skipped++
			continue
		}
		pending = append(pending, pendingOffer{listing: listing, offer: *offer, hash: hash})
	}

	pushed, failed := 0, 0
	for _, batch := range chunkOffers(pending, client.MaxBatchSize()) {
		offers := make([]MarketplaceOfferUpdate, len(batch))
		for i, entry := range batch {
			offers[i] = entry.offer
		}
		result := client.UpdateOffers(ctx, credentials, offers)
		for _, entry := range batch {
			if result.IsSuccess() {
				if markResult := entry.listing.MarkOfferSynced(entry.hash, now); markResult.IsSuccess() {
					pushed++
				} else {
					failed++
				}
			} else {
				entry.listing.RegisterSyncFailure(nextOfferAttempt(entry.listing.SyncAttempts, now))
				failed++
			}
			if err := s.store.Update(ctx, entry.listing); err != nil {
				return sharedkernel.FailOf[OfferSyncSummary](sharedkernel.NewInternalError(err.Error()))
			}
		}
		if result.IsFailure() {
			slog.Warn("Teklif gönderimi başarısız.",
				slog.String("Marketplace", marketplaceCode), slog.Int("Count", len(batch)),
				slog.String("Error", result.Err().Message))
		}
	}

	return sharedkernel.OkOf(OfferSyncSummary{
		Examined: len(candidates), Skipped: skipped, Pushed: pushed, Failed: failed})
}

// buildOffer, listeleme + fiyat + stoktan teklif günceli kurar; fiyatı
// kararlaştırılmamış ya da dış kimliği olmayan kalem için nil döner.
func buildOffer(listing *domain.ProductListing, priceByItem map[uuid.UUID]DecidedChannelPrice, quantityByItem map[uuid.UUID]int) *MarketplaceOfferUpdate {
	if listing.ExternalListingID == nil {
		return nil
	}
	price, ok := priceByItem[listing.ProductItemID]
	if !ok {
		return nil
	}
	// Stok kaydı yoksa kalem tükenmiş sayılır: pazaryerinde de sıfıra çekilmelidir.
	quantity := quantityByItem[listing.ProductItemID]
	return &MarketplaceOfferUpdate{
		ExternalListingID: *listing.ExternalListingID, Quantity: quantity,
		Amount: price.Amount, CompareAtAmount: price.CompareAtAmount, Currency: price.Currency,
	}
}

// nextOfferAttempt, teklif senkron hatası sonrası bir sonraki deneme zamanını
// hesaplar: 30s·2ⁿ, tavan 1 saat, n en fazla 10 (.NET NextAttemptAt portu).
func nextOfferAttempt(attempts int, now time.Time) time.Time {
	return now.Add(backoffDelay(attempts, 10, offerBaseBackoff, offerMaxBackoff))
}

// backoffDelay, üstel backoff süresini hesaplar; taşmayı önlemek için tavana
// göre sınırlanır.
func backoffDelay(attempts, maxFactor int, base, cap time.Duration) time.Duration {
	factor := attempts
	if factor > maxFactor {
		factor = maxFactor
	}
	delay := time.Duration(float64(base) * math.Pow(2, float64(factor)))
	if delay > cap || delay <= 0 {
		return cap
	}
	return delay
}

// chunkOffers, listeyi verilen boyutta partilere böler.
func chunkOffers[T any](source []T, size int) [][]T {
	if size < 1 {
		size = 1
	}
	chunks := make([][]T, 0, (len(source)+size-1)/size)
	for start := 0; start < len(source); start += size {
		end := start + size
		if end > len(source) {
			end = len(source)
		}
		chunks = append(chunks, source[start:end])
	}
	return chunks
}
