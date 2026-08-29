package domain

import (
	"strings"
	"time"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// ListingStatus, kalemin pazaryerindeki listeleme yaşam döngüsü durumudur.
// PublicationStatus bir iş (job) durumudur; bu ise ilişkinin güncel durumudur
// ve işlerden bağımsız yaşar. Veritabanında snake_case saklanır.
type ListingStatus string

// Listeleme durumları.
const (
	ListingPending       ListingStatus = "pending"
	ListingSubmitted     ListingStatus = "submitted"
	ListingLive          ListingStatus = "live"
	ListingRejected      ListingStatus = "rejected"
	ListingPendingDelist ListingStatus = "pending_delist"
	ListingDelisted      ListingStatus = "delisted"
)

// Listeleme alan sınırları.
const (
	ExternalListingIDMaxLength   = 200
	SubmissionReferenceMaxLength = 200
	ListingHashMaxLength         = 64
	RejectionReasonMaxLength     = 1000
)

// ProductListing, bir satılabilir kalemin bir pazaryerindeki kalıcı listeleme
// durumudur (.NET ProductListing aggregate'i). Kanonik ProductItemID ile
// pazaryerindeki ExternalListingID arasındaki köprüdür; ikinci gönderimin
// "güncelle" olmasını ve yalnız değişenin push edilmesini sağlar.
//
// İçerik ve teklif ayrımı: fiyat/stok güncellemesi ucuzdur ve yeniden onaya
// girmez; içerik güncellemesi pahalıdır ve onay kuyruğuna düşürür. Bu yüzden
// iki ayrı hash ve iki ayrı kirlilik damgası tutulur — stok değişimi asla
// içerik güncellemesi tetiklemez. Doğal anahtar: (tenant, pazaryeri, kalem).
type ProductListing struct {
	ID                  uuid.UUID
	TenantID            uuid.UUID
	MarketplaceCode     string
	ProductItemID       uuid.UUID
	Status              ListingStatus
	ExternalListingID   *string
	SubmissionReference *string
	ContentHash         *string
	OfferHash           *string
	ContentDirtyAt      *time.Time
	OfferDirtyAt        *time.Time
	LastSubmittedAt     *time.Time
	LastConfirmedAt     *time.Time
	RejectionReason     *string
	SyncAttempts        int
	NextAttemptAt       *time.Time
}

// ListingSyncScope, gönderim bekleyen bir (tenant, pazaryeri) çiftidir
// (.NET ListingSyncScope karşılığı; listing-sync worker'ının keşif adımı bu
// kapsamları tenant bağlamı olmadan tarar).
type ListingSyncScope struct {
	TenantID        uuid.UUID
	MarketplaceCode string
}

// truncate, değeri kırpar ve sınıra indirger; boş değer nil olur.
func truncate(value string, maxLength int) *string {
	trimmed := strings.TrimSpace(value)
	if trimmed == "" {
		return nil
	}
	if len([]rune(trimmed)) > maxLength {
		trimmed = string([]rune(trimmed)[:maxLength])
	}
	return &trimmed
}

// NewListing, yayınlanmak üzere yeni listeleme kaydı açar: pending durumda ve
// hash'ler bilinmediği için kirli başlar.
func NewListing(tenantID uuid.UUID, marketplaceCode string, productItemID uuid.UUID, createdAt time.Time) sharedkernel.ResultOf[*ProductListing] {
	if tenantID == uuid.Nil {
		return sharedkernel.FailOf[*ProductListing](sharedkernel.NewValidationError("Tenant id is required."))
	}
	if productItemID == uuid.Nil {
		return sharedkernel.FailOf[*ProductListing](sharedkernel.NewValidationError("Product item id is required."))
	}
	dirty := createdAt
	return sharedkernel.OkOf(&ProductListing{
		ID: uuid.New(), TenantID: tenantID, MarketplaceCode: marketplaceCode,
		ProductItemID: productItemID, Status: ListingPending,
		ContentDirtyAt: &dirty, OfferDirtyAt: &dirty,
	})
}

// SeedListing, pazaryerinde zaten var olan bir listelemeyi kaydeder (import
// ile keşfedilen kalemler): live başlar, hash'ler ilk senkron turunda uzlaşır.
func SeedListing(tenantID uuid.UUID, marketplaceCode string, productItemID uuid.UUID, externalListingID string, discoveredAt time.Time) sharedkernel.ResultOf[*ProductListing] {
	created := NewListing(tenantID, marketplaceCode, productItemID, discoveredAt)
	if created.IsFailure() {
		return created
	}
	normalized := truncate(externalListingID, ExternalListingIDMaxLength)
	if normalized == nil {
		return sharedkernel.FailOf[*ProductListing](sharedkernel.NewValidationError("External listing id is required."))
	}
	listing := created.Value()
	listing.Status = ListingLive
	listing.ExternalListingID = normalized
	confirmedAt := discoveredAt
	listing.LastConfirmedAt = &confirmedAt
	return sharedkernel.OkOf(listing)
}

// MarkContentDirty, içeriğin değiştiğini işaretler; idempotenttir, ilk damga korunur.
func (l *ProductListing) MarkContentDirty(at time.Time) {
	if l.ContentDirtyAt == nil {
		l.ContentDirtyAt = &at
	}
}

// MarkOfferDirty, fiyat/stoğun değiştiğini işaretler; idempotenttir.
func (l *ProductListing) MarkOfferDirty(at time.Time) {
	if l.OfferDirtyAt == nil {
		l.OfferDirtyAt = &at
	}
}

// NeedsContentSync, verilen içerik hash'i için gönderim gerekli mi; hash
// aynıysa pazaryerine hiç çağrı yapılmaz.
func (l *ProductListing) NeedsContentSync(contentHash string) bool {
	if l.Status == ListingPendingDelist || l.Status == ListingDelisted {
		return false
	}
	return l.ContentHash == nil || *l.ContentHash != contentHash
}

// NeedsOfferSync, verilen teklif hash'i için gönderim gerekli mi; teklif
// güncellemesi yalnızca dış kimliği bilinen listelemeler için anlamlıdır.
func (l *ProductListing) NeedsOfferSync(offerHash string) bool {
	if l.ExternalListingID == nil || l.Status == ListingPendingDelist || l.Status == ListingDelisted {
		return false
	}
	return l.OfferHash == nil || *l.OfferHash != offerHash
}

// IsSyncDue, backoff penceresinin dolup dolmadığını döner.
func (l *ProductListing) IsSyncDue(now time.Time) bool {
	return l.NextAttemptAt == nil || !l.NextAttemptAt.After(now)
}

// MarkContentSubmitted, içerik gönderimini kaydeder: hash saklanır, kirlilik
// temizlenir, listeleme onay bekler duruma geçer.
func (l *ProductListing) MarkContentSubmitted(contentHash string, submissionReference *string, at time.Time) sharedkernel.Result {
	if l.Status == ListingPendingDelist || l.Status == ListingDelisted {
		return sharedkernel.Fail(sharedkernel.NewConflictError("Delisted listings cannot be submitted."))
	}
	normalized := truncate(contentHash, ListingHashMaxLength)
	if normalized == nil {
		return sharedkernel.Fail(sharedkernel.NewValidationError("Content hash is required."))
	}
	l.ContentHash = normalized
	l.ContentDirtyAt = nil
	if submissionReference != nil {
		l.SubmissionReference = truncate(*submissionReference, SubmissionReferenceMaxLength)
	} else {
		l.SubmissionReference = nil
	}
	submitted := at
	l.LastSubmittedAt = &submitted
	l.Status = ListingSubmitted
	l.RejectionReason = nil
	l.resetBackoff()
	return sharedkernel.Ok()
}

// MarkOfferSynced, fiyat/stok gönderimini kaydeder; durumu DEĞİŞTİRMEZ (teklif
// güncellemesi yeniden onay tetiklemez, canlı listeleme canlı kalır).
func (l *ProductListing) MarkOfferSynced(offerHash string, at time.Time) sharedkernel.Result {
	if l.ExternalListingID == nil {
		return sharedkernel.Fail(sharedkernel.NewConflictError("Offer sync requires a published listing."))
	}
	normalized := truncate(offerHash, ListingHashMaxLength)
	if normalized == nil {
		return sharedkernel.Fail(sharedkernel.NewValidationError("Offer hash is required."))
	}
	l.OfferHash = normalized
	l.OfferDirtyAt = nil
	submitted := at
	l.LastSubmittedAt = &submitted
	l.resetBackoff()
	return sharedkernel.Ok()
}

// MarkLive, pazaryerinin listelemeyi kabul ettiğini kaydeder.
func (l *ProductListing) MarkLive(externalListingID string, at time.Time) sharedkernel.Result {
	normalized := truncate(externalListingID, ExternalListingIDMaxLength)
	if normalized == nil {
		return sharedkernel.Fail(sharedkernel.NewValidationError("External listing id is required."))
	}
	l.ExternalListingID = normalized
	l.Status = ListingLive
	confirmed := at
	l.LastConfirmedAt = &confirmed
	l.RejectionReason = nil
	l.resetBackoff()
	return sharedkernel.Ok()
}

// MarkRejected, pazaryerinin İÇERİĞİ reddettiğini kaydeder; saklanan içerik
// hash'i artık pazaryerini temsil etmediği için sıfırlanır ve içerik yeniden
// kirli işaretlenir. Altyapı hataları için RegisterSyncFailure kullanılır.
func (l *ProductListing) MarkRejected(reason string, at time.Time) sharedkernel.Result {
	if strings.TrimSpace(reason) == "" {
		return sharedkernel.Fail(sharedkernel.NewValidationError("Rejection reason is required."))
	}
	l.Status = ListingRejected
	l.RejectionReason = truncate(reason, RejectionReasonMaxLength)
	confirmed := at
	l.LastConfirmedAt = &confirmed
	l.ContentHash = nil
	dirty := at
	l.ContentDirtyAt = &dirty
	return sharedkernel.Ok()
}

// RegisterSyncFailure, geçici (taşıma/altyapı) senkron hatasını kaydeder:
// durum korunur, kirlilik temizlenmez, sonraki deneme ertelenir.
func (l *ProductListing) RegisterSyncFailure(nextAttemptAt time.Time) {
	l.SyncAttempts++
	l.NextAttemptAt = &nextAttemptAt
}

// resetBackoff, başarıda deneme sayaçlarını sıfırlar.
func (l *ProductListing) resetBackoff() {
	l.SyncAttempts = 0
	l.NextAttemptAt = nil
}
