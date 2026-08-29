package infrastructure

import (
	"context"
	"errors"
	"fmt"
	"time"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"

	"pimly.commerslab/backend-go/internal/integration/trendyol"
	"pimly.commerslab/backend-go/internal/modules/channels/domain"
)

// Bu dosya, worker'ların (taksonomi/import/yayın) kuyruk claim ve toplu yazma
// operasyonlarını içerir. Claim'ler .NET ile aynı deseni izler: transaction
// içinde FOR UPDATE SKIP LOCKED ile satır kilitlenir, running'e çekilir ve
// commit edilir — birden çok worker instance'ı güvenle yarışabilir.

// GetAnyEnabledConnection, pazaryeri için etkin HERHANGİ bir tenant'ın
// bağlantısını döner (taksonomi/attribute uçları pazaryeri-globaldir; gizli
// anahtarı dolu bağlantı tercih edilir — .NET GetAnyEnabledAsync portu).
func (r *Repository) GetAnyEnabledConnection(ctx context.Context, marketplaceCode string) (*domain.MarketplaceConnection, error) {
	var c domain.MarketplaceConnection
	err := r.pool.QueryRow(ctx,
		`SELECT id, marketplace_code, seller_id, api_key, api_secret, is_enabled
		 FROM channels.marketplace_connections
		 WHERE marketplace_code = $1 AND is_enabled = TRUE
		 ORDER BY (api_secret IS NOT NULL) DESC LIMIT 1`, marketplaceCode).
		Scan(&c.ID, &c.MarketplaceCode, &c.SellerID, &c.ApiKey, &c.ApiSecret, &c.IsEnabled)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("channels: etkin bağlantı okunamadı: %w", err)
	}
	return &c, nil
}

// ClaimNextPendingTaxonomyRun, sıradaki pending taksonomi işini claim edip
// running durumuna alır; kuyruk boşsa nil döner.
func (r *Repository) ClaimNextPendingTaxonomyRun(ctx context.Context) (*domain.TaxonomySyncRun, error) {
	tx, err := r.pool.Begin(ctx)
	if err != nil {
		return nil, fmt.Errorf("channels: taksonomi claim işlemi başlatılamadı: %w", err)
	}
	defer func() { _ = tx.Rollback(ctx) }()

	var id uuid.UUID
	err = tx.QueryRow(ctx,
		`SELECT id FROM channels.taxonomy_sync_runs
		 WHERE status = 'pending' ORDER BY created_at LIMIT 1 FOR UPDATE SKIP LOCKED`).Scan(&id)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("channels: taksonomi işi claim edilemedi: %w", err)
	}

	run, err := scanTaxonomyRun(tx.QueryRow(ctx,
		`SELECT `+taxonomyRunColumns+` FROM channels.taxonomy_sync_runs WHERE id = $1`, id))
	if err != nil || run == nil {
		return nil, err
	}
	if markResult := run.MarkRunning(time.Now().UTC()); markResult.IsFailure() {
		return nil, nil
	}
	if err := updateTaxonomyRunTx(ctx, tx, run); err != nil {
		return nil, err
	}
	if err := tx.Commit(ctx); err != nil {
		return nil, err
	}
	return run, nil
}

// updateTaxonomyRunTx, iş kaydını verilen transaction'da kalıcılaştırır.
func updateTaxonomyRunTx(ctx context.Context, tx pgx.Tx, run *domain.TaxonomySyncRun) error {
	_, err := tx.Exec(ctx,
		`UPDATE channels.taxonomy_sync_runs SET status = $2, started_at = $3, completed_at = $4,
		   processed_count = $5, total_estimate = $6, error_message = $7
		 WHERE id = $1`,
		run.ID, string(run.Status), run.StartedAt, run.CompletedAt,
		run.ProcessedCount, run.TotalEstimate, run.ErrorMessage)
	if err != nil {
		return fmt.Errorf("channels: taksonomi işi güncellenemedi: %w", err)
	}
	return nil
}

// UpdateTaxonomyRun, iş kaydını kalıcılaştırır.
func (r *Repository) UpdateTaxonomyRun(ctx context.Context, run *domain.TaxonomySyncRun) error {
	_, err := r.pool.Exec(ctx,
		`UPDATE channels.taxonomy_sync_runs SET status = $2, started_at = $3, completed_at = $4,
		   processed_count = $5, total_estimate = $6, error_message = $7
		 WHERE id = $1`,
		run.ID, string(run.Status), run.StartedAt, run.CompletedAt,
		run.ProcessedCount, run.TotalEstimate, run.ErrorMessage)
	if err != nil {
		return fmt.Errorf("channels: taksonomi işi güncellenemedi: %w", err)
	}
	return nil
}

// HasTaxonomyRunSince, verilen andan sonra oluşturulmuş herhangi bir işin var
// olup olmadığını döner (zamanlanmış senkronun "bu slotta zaten koştu" denetimi).
func (r *Repository) HasTaxonomyRunSince(ctx context.Context, marketplaceCode string, since time.Time) (bool, error) {
	var exists bool
	err := r.pool.QueryRow(ctx,
		`SELECT EXISTS (SELECT 1 FROM channels.taxonomy_sync_runs
		  WHERE marketplace_code = $1 AND created_at >= $2)`, marketplaceCode, since).Scan(&exists)
	return exists, err
}

// UpsertExternalCategoriesBatch, düzleştirilmiş kategori partisini
// (marketplace_code, external_id) doğal anahtarıyla ekler/günceller.
func (r *Repository) UpsertExternalCategoriesBatch(ctx context.Context, marketplaceCode string, nodes []trendyol.CategoryNode, syncedAt time.Time) error {
	if len(nodes) == 0 {
		return nil
	}
	tx, err := r.pool.Begin(ctx)
	if err != nil {
		return fmt.Errorf("channels: kategori upsert işlemi başlatılamadı: %w", err)
	}
	defer func() { _ = tx.Rollback(ctx) }()

	for _, node := range nodes {
		if _, err := tx.Exec(ctx,
			`INSERT INTO channels.external_categories
			   (id, marketplace_code, external_id, name, parent_external_id, path, is_leaf, synced_at)
			 VALUES ($1, $2, $3, $4, $5, $6, $7, $8)
			 ON CONFLICT (marketplace_code, external_id) DO UPDATE SET
			   name = $4, parent_external_id = $5, path = $6, is_leaf = $7, synced_at = $8`,
			uuid.New(), marketplaceCode, node.ExternalID, node.Name,
			node.ParentExternalID, node.Path, node.IsLeaf, syncedAt); err != nil {
			return fmt.Errorf("channels: kategori upsert edilemedi: %w", err)
		}
	}
	return tx.Commit(ctx)
}
