package infrastructure

import (
	"context"
	"fmt"
	"time"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgxpool"

	"pimly.commerslab/backend-go/internal/modules/channels/domain"
)

// ListingRepository, channels.product_listings tablosunun pgx uygulamasıdır.
// Kuyruk/senkron tarafında kullanılır: dispatcher kirli işaretler, listing-sync
// worker'ı kirli kapsamları keşfedip hash kapılı gönderim yapar.
type ListingRepository struct {
	pool *pgxpool.Pool
}

// NewListingRepository, verilen havuzla listeleme deposunu oluşturur.
func NewListingRepository(pool *pgxpool.Pool) *ListingRepository {
	return &ListingRepository{pool: pool}
}

const listingColumns = `id, tenant_id, marketplace_code, product_item_id, status,
	external_listing_id, submission_reference, content_hash, offer_hash,
	content_dirty_at, offer_dirty_at, last_submitted_at, last_confirmed_at,
	rejection_reason, sync_attempts, next_attempt_at`

// scanListing, tek listeleme satırını okur.
func scanListing(row pgx.Row) (*domain.ProductListing, error) {
	var l domain.ProductListing
	var status string
	err := row.Scan(&l.ID, &l.TenantID, &l.MarketplaceCode, &l.ProductItemID, &status,
		&l.ExternalListingID, &l.SubmissionReference, &l.ContentHash, &l.OfferHash,
		&l.ContentDirtyAt, &l.OfferDirtyAt, &l.LastSubmittedAt, &l.LastConfirmedAt,
		&l.RejectionReason, &l.SyncAttempts, &l.NextAttemptAt)
	if err != nil {
		return nil, err
	}
	l.Status = domain.ListingStatus(status)
	return &l, nil
}

// MarkDirtyByItem, kalemin listelemelerini kirli işaretler ve etkilenen satır
// sayısını döner (.NET MarkListingsDirtyHandler'ın SQL karşılığı; MarkDirty
// idempotenttir — COALESCE ilk damgayı korur). marketplaceCode nil ise kalemin
// tüm listelemeleri işaretlenir. Dispatcher tenant'sız çalıştığından tenant,
// satırın kendisinden gelir (product_item_id kalem başına tenant'a özgüdür).
func (r *ListingRepository) MarkDirtyByItem(ctx context.Context, tx pgx.Tx, productItemID uuid.UUID, marketplaceCode *string, markContent, markOffer bool, at time.Time) (int, error) {
	set := []string{}
	if markContent {
		set = append(set, "content_dirty_at = COALESCE(content_dirty_at, $2)")
	}
	if markOffer {
		set = append(set, "offer_dirty_at = COALESCE(offer_dirty_at, $2)")
	}
	if len(set) == 0 {
		return 0, nil
	}

	sql := "UPDATE channels.product_listings SET " + set[0]
	if len(set) == 2 {
		sql += ", " + set[1]
	}
	sql += " WHERE product_item_id = $1"
	args := []any{productItemID, at}
	if marketplaceCode != nil {
		sql += " AND marketplace_code = $3"
		args = append(args, *marketplaceCode)
	}

	tag, err := tx.Exec(ctx, sql, args...)
	if err != nil {
		return 0, fmt.Errorf("channels: listelemeler kirli işaretlenemedi: %w", err)
	}
	return int(tag.RowsAffected()), nil
}

// ListDirtyScopes, gönderim bekleyen (tenant, pazaryeri) çiftlerini keşfeder;
// backoff penceresi dolmamış satırlar kapsam dışıdır (.NET ListDirtyScopesAsync).
func (r *ListingRepository) ListDirtyScopes(ctx context.Context, tenantFilter []uuid.UUID, now time.Time) ([]struct {
	TenantID        uuid.UUID
	MarketplaceCode string
}, error) {
	sql := `SELECT DISTINCT tenant_id, marketplace_code FROM channels.product_listings
	        WHERE (content_dirty_at IS NOT NULL OR offer_dirty_at IS NOT NULL)
	          AND (next_attempt_at IS NULL OR next_attempt_at <= $1)`
	args := []any{now}
	if len(tenantFilter) > 0 {
		sql += ` AND tenant_id = ANY($2)`
		args = append(args, tenantFilter)
	}
	rows, err := r.pool.Query(ctx, sql, args...)
	if err != nil {
		return nil, fmt.Errorf("channels: kirli kapsamlar keşfedilemedi: %w", err)
	}
	defer rows.Close()

	scopes := []struct {
		TenantID        uuid.UUID
		MarketplaceCode string
	}{}
	for rows.Next() {
		var scope struct {
			TenantID        uuid.UUID
			MarketplaceCode string
		}
		if err := rows.Scan(&scope.TenantID, &scope.MarketplaceCode); err != nil {
			return nil, err
		}
		scopes = append(scopes, scope)
	}
	return scopes, rows.Err()
}

// ListDirty, kapsamdaki gönderim bekleyen listelemeleri döner; backoff'u
// dolmamışlar atlanır.
func (r *ListingRepository) ListDirty(ctx context.Context, tenantID uuid.UUID, marketplaceCode string, now time.Time, limit int) ([]*domain.ProductListing, error) {
	rows, err := r.pool.Query(ctx,
		`SELECT `+listingColumns+` FROM channels.product_listings
		 WHERE tenant_id = $1 AND marketplace_code = $2
		   AND (content_dirty_at IS NOT NULL OR offer_dirty_at IS NOT NULL)
		   AND (next_attempt_at IS NULL OR next_attempt_at <= $3)
		 ORDER BY offer_dirty_at NULLS LAST, content_dirty_at NULLS LAST
		 LIMIT $4`, tenantID, marketplaceCode, now, limit)
	if err != nil {
		return nil, fmt.Errorf("channels: kirli listelemeler okunamadı: %w", err)
	}
	defer rows.Close()

	items := []*domain.ProductListing{}
	for rows.Next() {
		listing, err := scanListing(rows)
		if err != nil {
			return nil, err
		}
		items = append(items, listing)
	}
	return items, rows.Err()
}

// GetByItem, kalem + pazaryeri için listelemeyi döner; yoksa nil.
func (r *ListingRepository) GetByItem(ctx context.Context, tenantID uuid.UUID, marketplaceCode string, productItemID uuid.UUID) (*domain.ProductListing, error) {
	listing, err := scanListing(r.pool.QueryRow(ctx,
		`SELECT `+listingColumns+` FROM channels.product_listings
		 WHERE tenant_id = $1 AND marketplace_code = $2 AND product_item_id = $3`,
		tenantID, marketplaceCode, productItemID))
	if err != nil {
		if err == pgx.ErrNoRows {
			return nil, nil
		}
		return nil, fmt.Errorf("channels: listeleme okunamadı: %w", err)
	}
	return listing, nil
}

// Upsert, listelemeyi doğal anahtar üzerinden ekler/günceller.
func (r *ListingRepository) Upsert(ctx context.Context, l *domain.ProductListing) error {
	_, err := r.pool.Exec(ctx,
		`INSERT INTO channels.product_listings
		   (id, tenant_id, marketplace_code, product_item_id, status, external_listing_id,
		    submission_reference, content_hash, offer_hash, content_dirty_at, offer_dirty_at,
		    last_submitted_at, last_confirmed_at, rejection_reason, sync_attempts, next_attempt_at)
		 VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,$12,$13,$14,$15,$16)
		 ON CONFLICT (tenant_id, marketplace_code, product_item_id) DO UPDATE SET
		   status = $5, external_listing_id = $6, submission_reference = $7,
		   content_hash = $8, offer_hash = $9, content_dirty_at = $10, offer_dirty_at = $11,
		   last_submitted_at = $12, last_confirmed_at = $13, rejection_reason = $14,
		   sync_attempts = $15, next_attempt_at = $16`,
		l.ID, l.TenantID, l.MarketplaceCode, l.ProductItemID, string(l.Status),
		l.ExternalListingID, l.SubmissionReference, l.ContentHash, l.OfferHash,
		l.ContentDirtyAt, l.OfferDirtyAt, l.LastSubmittedAt, l.LastConfirmedAt,
		l.RejectionReason, l.SyncAttempts, l.NextAttemptAt)
	if err != nil {
		return fmt.Errorf("channels: listeleme yazılamadı: %w", err)
	}
	return nil
}

// ListByProductItems, kalemlerin bu pazaryerindeki mevcut listelemelerini döner
// (.NET ListByProductItemsAsync portu; import backfill denetiminde kullanılır).
func (r *ListingRepository) ListByProductItems(ctx context.Context, tenantID uuid.UUID, marketplaceCode string, productItemIDs []uuid.UUID) ([]*domain.ProductListing, error) {
	if len(productItemIDs) == 0 {
		return []*domain.ProductListing{}, nil
	}
	rows, err := r.pool.Query(ctx,
		`SELECT `+listingColumns+` FROM channels.product_listings
		 WHERE tenant_id = $1 AND marketplace_code = $2 AND product_item_id = ANY($3)`,
		tenantID, marketplaceCode, productItemIDs)
	if err != nil {
		return nil, fmt.Errorf("channels: listelemeler okunamadı: %w", err)
	}
	defer rows.Close()

	items := []*domain.ProductListing{}
	for rows.Next() {
		listing, err := scanListing(rows)
		if err != nil {
			return nil, err
		}
		items = append(items, listing)
	}
	return items, rows.Err()
}

// AddRange, yeni listeleme kayıtlarını tek transaction'da ekler; doğal anahtar
// çakışmasında satır atlanır (eşzamanlı tohumlama idempotent kalır).
func (r *ListingRepository) AddRange(ctx context.Context, listings []*domain.ProductListing) error {
	if len(listings) == 0 {
		return nil
	}
	tx, err := r.pool.Begin(ctx)
	if err != nil {
		return fmt.Errorf("channels: listeleme ekleme işlemi başlatılamadı: %w", err)
	}
	defer func() { _ = tx.Rollback(ctx) }()

	for _, l := range listings {
		if _, err := tx.Exec(ctx,
			`INSERT INTO channels.product_listings
			   (id, tenant_id, marketplace_code, product_item_id, status, external_listing_id,
			    submission_reference, content_hash, offer_hash, content_dirty_at, offer_dirty_at,
			    last_submitted_at, last_confirmed_at, rejection_reason, sync_attempts, next_attempt_at)
			 VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,$12,$13,$14,$15,$16)
			 ON CONFLICT (tenant_id, marketplace_code, product_item_id) DO NOTHING`,
			l.ID, l.TenantID, l.MarketplaceCode, l.ProductItemID, string(l.Status),
			l.ExternalListingID, l.SubmissionReference, l.ContentHash, l.OfferHash,
			l.ContentDirtyAt, l.OfferDirtyAt, l.LastSubmittedAt, l.LastConfirmedAt,
			l.RejectionReason, l.SyncAttempts, l.NextAttemptAt); err != nil {
			return fmt.Errorf("channels: listeleme eklenemedi: %w", err)
		}
	}
	return tx.Commit(ctx)
}

// Update, mevcut listelemeyi kimliğiyle kalıcılaştırır.
func (r *ListingRepository) Update(ctx context.Context, l *domain.ProductListing) error {
	_, err := r.pool.Exec(ctx,
		`UPDATE channels.product_listings SET
		   status = $2, external_listing_id = $3, submission_reference = $4,
		   content_hash = $5, offer_hash = $6, content_dirty_at = $7, offer_dirty_at = $8,
		   last_submitted_at = $9, last_confirmed_at = $10, rejection_reason = $11,
		   sync_attempts = $12, next_attempt_at = $13
		 WHERE id = $1`,
		l.ID, string(l.Status), l.ExternalListingID, l.SubmissionReference,
		l.ContentHash, l.OfferHash, l.ContentDirtyAt, l.OfferDirtyAt,
		l.LastSubmittedAt, l.LastConfirmedAt, l.RejectionReason,
		l.SyncAttempts, l.NextAttemptAt)
	if err != nil {
		return fmt.Errorf("channels: listeleme güncellenemedi: %w", err)
	}
	return nil
}
