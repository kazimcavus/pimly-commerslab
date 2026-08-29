// Package infrastructure, Channels modülünün pgx kalıcılık katmanını içerir.
// Taksonomi işleri ve harici katalog cache'i pazaryeri-global tablolardır
// (tenant kolonu yok); bağlantılar, eşlemeler ve import/yayın işleri tenant'lıdır.
package infrastructure

import (
	"context"
	"errors"
	"fmt"
	"strings"
	"time"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgxpool"

	"pimly.commerslab/backend-go/internal/modules/channels/application"
	"pimly.commerslab/backend-go/internal/modules/channels/domain"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// Repository, channels şemasının pgx uygulamasıdır.
type Repository struct {
	pool *pgxpool.Pool
}

// NewRepository, verilen havuzla depoyu oluşturur.
func NewRepository(pool *pgxpool.Pool) *Repository {
	return &Repository{pool: pool}
}

// --- Bağlantılar ---

// GetConnection, tenant'ın pazaryeri bağlantısını döner; yoksa nil.
func (r *Repository) GetConnection(ctx context.Context, tenantID uuid.UUID, marketplaceCode string) (*domain.MarketplaceConnection, error) {
	var c domain.MarketplaceConnection
	err := r.pool.QueryRow(ctx,
		`SELECT id, marketplace_code, seller_id, api_key, api_secret, is_enabled
		 FROM channels.marketplace_connections
		 WHERE tenant_id = $1 AND marketplace_code = $2`, tenantID, marketplaceCode).
		Scan(&c.ID, &c.MarketplaceCode, &c.SellerID, &c.ApiKey, &c.ApiSecret, &c.IsEnabled)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("channels: bağlantı okunamadı: %w", err)
	}
	return &c, nil
}

// GetConfiguredMarketplaceCodes, tenant'ın bağlantısı olan pazaryeri kodlarını döner.
func (r *Repository) GetConfiguredMarketplaceCodes(ctx context.Context, tenantID uuid.UUID) (map[string]struct{}, error) {
	rows, err := r.pool.Query(ctx,
		`SELECT marketplace_code FROM channels.marketplace_connections WHERE tenant_id = $1`, tenantID)
	if err != nil {
		return nil, fmt.Errorf("channels: bağlantılar okunamadı: %w", err)
	}
	defer rows.Close()

	codes := map[string]struct{}{}
	for rows.Next() {
		var code string
		if err := rows.Scan(&code); err != nil {
			return nil, err
		}
		codes[code] = struct{}{}
	}
	return codes, rows.Err()
}

// AddConnection, yeni bağlantıyı ekler.
func (r *Repository) AddConnection(ctx context.Context, tenantID uuid.UUID, c *domain.MarketplaceConnection) error {
	_, err := r.pool.Exec(ctx,
		`INSERT INTO channels.marketplace_connections
		   (id, tenant_id, marketplace_code, seller_id, api_key, api_secret, is_enabled)
		 VALUES ($1, $2, $3, $4, $5, $6, $7)`,
		c.ID, tenantID, c.MarketplaceCode, c.SellerID, c.ApiKey, c.ApiSecret, c.IsEnabled)
	if err != nil {
		return fmt.Errorf("channels: bağlantı eklenemedi: %w", err)
	}
	return nil
}

// UpdateConnection, bağlantıyı kalıcılaştırır.
func (r *Repository) UpdateConnection(ctx context.Context, tenantID uuid.UUID, c *domain.MarketplaceConnection) error {
	_, err := r.pool.Exec(ctx,
		`UPDATE channels.marketplace_connections
		 SET seller_id = $3, api_key = $4, api_secret = $5, is_enabled = $6
		 WHERE tenant_id = $1 AND id = $2`,
		tenantID, c.ID, c.SellerID, c.ApiKey, c.ApiSecret, c.IsEnabled)
	if err != nil {
		return fmt.Errorf("channels: bağlantı güncellenemedi: %w", err)
	}
	return nil
}

// --- Taksonomi senkron işleri (pazaryeri-global) ---

const taxonomyRunColumns = `id, marketplace_code, status, created_at, started_at,
	completed_at, processed_count, total_estimate, error_message`

// scanTaxonomyRun, tek iş satırını okur.
func scanTaxonomyRun(row pgx.Row) (*domain.TaxonomySyncRun, error) {
	var run domain.TaxonomySyncRun
	var status string
	err := row.Scan(&run.ID, &run.MarketplaceCode, &status, &run.CreatedAt, &run.StartedAt,
		&run.CompletedAt, &run.ProcessedCount, &run.TotalEstimate, &run.ErrorMessage)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("channels: taksonomi işi okunamadı: %w", err)
	}
	run.Status = domain.TaxonomySyncStatus(status)
	return &run, nil
}

// GetActiveTaxonomyRun, pending/running işi döner; yoksa nil.
func (r *Repository) GetActiveTaxonomyRun(ctx context.Context, marketplaceCode string) (*domain.TaxonomySyncRun, error) {
	return scanTaxonomyRun(r.pool.QueryRow(ctx,
		`SELECT `+taxonomyRunColumns+` FROM channels.taxonomy_sync_runs
		 WHERE marketplace_code = $1 AND status IN ('pending', 'running')
		 ORDER BY created_at DESC LIMIT 1`, marketplaceCode))
}

// GetLatestCompletedTaxonomyRun, en son tamamlanan işi döner; yoksa nil.
func (r *Repository) GetLatestCompletedTaxonomyRun(ctx context.Context, marketplaceCode string) (*domain.TaxonomySyncRun, error) {
	return scanTaxonomyRun(r.pool.QueryRow(ctx,
		`SELECT `+taxonomyRunColumns+` FROM channels.taxonomy_sync_runs
		 WHERE marketplace_code = $1 AND status = 'completed'
		 ORDER BY completed_at DESC LIMIT 1`, marketplaceCode))
}

// GetTaxonomyRun, kimlikle işi döner; yoksa nil.
func (r *Repository) GetTaxonomyRun(ctx context.Context, id uuid.UUID) (*domain.TaxonomySyncRun, error) {
	return scanTaxonomyRun(r.pool.QueryRow(ctx,
		`SELECT `+taxonomyRunColumns+` FROM channels.taxonomy_sync_runs WHERE id = $1`, id))
}

// AddTaxonomyRun, yeni işi ekler.
func (r *Repository) AddTaxonomyRun(ctx context.Context, run *domain.TaxonomySyncRun) error {
	_, err := r.pool.Exec(ctx,
		`INSERT INTO channels.taxonomy_sync_runs
		   (id, marketplace_code, status, created_at, started_at, completed_at,
		    processed_count, total_estimate, error_message)
		 VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9)`,
		run.ID, run.MarketplaceCode, string(run.Status), run.CreatedAt, run.StartedAt,
		run.CompletedAt, run.ProcessedCount, run.TotalEstimate, run.ErrorMessage)
	if err != nil {
		return fmt.Errorf("channels: taksonomi işi eklenemedi: %w", err)
	}
	return nil
}

// --- Harici katalog cache'i (pazaryeri-global) ---

// CountExternalCategories, cache'lenmiş kategori sayısını döner.
func (r *Repository) CountExternalCategories(ctx context.Context, marketplaceCode string) (int, error) {
	var count int
	err := r.pool.QueryRow(ctx,
		`SELECT count(*) FROM channels.external_categories WHERE marketplace_code = $1`,
		marketplaceCode).Scan(&count)
	return count, err
}

// SearchExternalCategories, ad/yol araması yapar; yol + ada göre sıralı ilk
// limit kaydı döner (.NET SearchAsync ile aynı ILIKE deseni).
func (r *Repository) SearchExternalCategories(ctx context.Context, marketplaceCode string, query *string, limit int) ([]*domain.ExternalCategory, error) {
	sql := `SELECT id, marketplace_code, external_id, name, parent_external_id, path, is_leaf, synced_at
	        FROM channels.external_categories WHERE marketplace_code = $1`
	args := []any{marketplaceCode}
	if query != nil && strings.TrimSpace(*query) != "" {
		sql += ` AND (name ILIKE $2 OR path ILIKE $2)`
		args = append(args, "%"+strings.TrimSpace(*query)+"%")
	}
	sql += fmt.Sprintf(` ORDER BY path, name LIMIT %d`, limit)

	rows, err := r.pool.Query(ctx, sql, args...)
	if err != nil {
		return nil, fmt.Errorf("channels: harici kategoriler aranamadı: %w", err)
	}
	defer rows.Close()

	items := []*domain.ExternalCategory{}
	for rows.Next() {
		var c domain.ExternalCategory
		if err := rows.Scan(&c.ID, &c.MarketplaceCode, &c.ExternalID, &c.Name,
			&c.ParentExternalID, &c.Path, &c.IsLeaf, &c.SyncedAt); err != nil {
			return nil, err
		}
		items = append(items, &c)
	}
	return items, rows.Err()
}

// GetExternalCategory, harici kimlikle cache kaydını döner; yoksa nil.
func (r *Repository) GetExternalCategory(ctx context.Context, marketplaceCode, externalID string) (*domain.ExternalCategory, error) {
	var c domain.ExternalCategory
	err := r.pool.QueryRow(ctx,
		`SELECT id, marketplace_code, external_id, name, parent_external_id, path, is_leaf, synced_at
		 FROM channels.external_categories WHERE marketplace_code = $1 AND external_id = $2`,
		marketplaceCode, externalID).
		Scan(&c.ID, &c.MarketplaceCode, &c.ExternalID, &c.Name, &c.ParentExternalID,
			&c.Path, &c.IsLeaf, &c.SyncedAt)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("channels: harici kategori okunamadı: %w", err)
	}
	return &c, nil
}

// ListExternalAttributes, kategorinin özellik cache'ini ada göre sıralı döner.
func (r *Repository) ListExternalAttributes(ctx context.Context, marketplaceCode, externalCategoryID string) ([]*domain.ExternalCategoryAttribute, error) {
	rows, err := r.pool.Query(ctx,
		`SELECT id, marketplace_code, external_category_id, external_attribute_id, name,
		        required, allow_custom, is_variant, is_slicer, synced_at
		 FROM channels.external_category_attributes
		 WHERE marketplace_code = $1 AND external_category_id = $2
		 ORDER BY name`, marketplaceCode, externalCategoryID)
	if err != nil {
		return nil, fmt.Errorf("channels: harici özellikler okunamadı: %w", err)
	}
	defer rows.Close()

	items := []*domain.ExternalCategoryAttribute{}
	for rows.Next() {
		var a domain.ExternalCategoryAttribute
		if err := rows.Scan(&a.ID, &a.MarketplaceCode, &a.ExternalCategoryID, &a.ExternalAttributeID,
			&a.Name, &a.Required, &a.AllowCustom, &a.IsVariant, &a.IsSlicer, &a.SyncedAt); err != nil {
			return nil, err
		}
		items = append(items, &a)
	}
	return items, rows.Err()
}

// GetExternalAttribute, tek özellik cache kaydını döner; yoksa nil.
func (r *Repository) GetExternalAttribute(ctx context.Context, marketplaceCode, externalCategoryID, externalAttributeID string) (*domain.ExternalCategoryAttribute, error) {
	var a domain.ExternalCategoryAttribute
	err := r.pool.QueryRow(ctx,
		`SELECT id, marketplace_code, external_category_id, external_attribute_id, name,
		        required, allow_custom, is_variant, is_slicer, synced_at
		 FROM channels.external_category_attributes
		 WHERE marketplace_code = $1 AND external_category_id = $2 AND external_attribute_id = $3`,
		marketplaceCode, externalCategoryID, externalAttributeID).
		Scan(&a.ID, &a.MarketplaceCode, &a.ExternalCategoryID, &a.ExternalAttributeID,
			&a.Name, &a.Required, &a.AllowCustom, &a.IsVariant, &a.IsSlicer, &a.SyncedAt)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("channels: harici özellik okunamadı: %w", err)
	}
	return &a, nil
}

// ListExternalValues, kategorinin değer cache'ini döner.
func (r *Repository) ListExternalValues(ctx context.Context, marketplaceCode, externalCategoryID string) ([]*domain.ExternalAttributeValue, error) {
	rows, err := r.pool.Query(ctx,
		`SELECT id, marketplace_code, external_category_id, external_attribute_id,
		        external_value_id, name, synced_at
		 FROM channels.external_attribute_values
		 WHERE marketplace_code = $1 AND external_category_id = $2
		 ORDER BY external_attribute_id, name`, marketplaceCode, externalCategoryID)
	if err != nil {
		return nil, fmt.Errorf("channels: harici değerler okunamadı: %w", err)
	}
	defer rows.Close()

	items := []*domain.ExternalAttributeValue{}
	for rows.Next() {
		var v domain.ExternalAttributeValue
		if err := rows.Scan(&v.ID, &v.MarketplaceCode, &v.ExternalCategoryID, &v.ExternalAttributeID,
			&v.ExternalValueID, &v.Name, &v.SyncedAt); err != nil {
			return nil, err
		}
		items = append(items, &v)
	}
	return items, rows.Err()
}

// GetExternalValue, tek değer cache kaydını döner; yoksa nil.
func (r *Repository) GetExternalValue(ctx context.Context, marketplaceCode, externalCategoryID, externalAttributeID, externalValueID string) (*domain.ExternalAttributeValue, error) {
	var v domain.ExternalAttributeValue
	err := r.pool.QueryRow(ctx,
		`SELECT id, marketplace_code, external_category_id, external_attribute_id,
		        external_value_id, name, synced_at
		 FROM channels.external_attribute_values
		 WHERE marketplace_code = $1 AND external_category_id = $2
		   AND external_attribute_id = $3 AND external_value_id = $4`,
		marketplaceCode, externalCategoryID, externalAttributeID, externalValueID).
		Scan(&v.ID, &v.MarketplaceCode, &v.ExternalCategoryID, &v.ExternalAttributeID,
			&v.ExternalValueID, &v.Name, &v.SyncedAt)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("channels: harici değer okunamadı: %w", err)
	}
	return &v, nil
}

// RefreshExternalAttributes, kategorinin özellik + değer cache'ini pazaryeri
// verisiyle tek transaction'da değiştirir (cache tazeleme: sil + yaz).
func (r *Repository) RefreshExternalAttributes(ctx context.Context, marketplaceCode, externalCategoryID string, nodes []application.MarketplaceCategoryAttributeNode, syncedAt time.Time) error {
	tx, err := r.pool.Begin(ctx)
	if err != nil {
		return fmt.Errorf("channels: cache tazeleme işlemi başlatılamadı: %w", err)
	}
	defer func() { _ = tx.Rollback(ctx) }()

	if _, err := tx.Exec(ctx,
		`DELETE FROM channels.external_attribute_values
		 WHERE marketplace_code = $1 AND external_category_id = $2`,
		marketplaceCode, externalCategoryID); err != nil {
		return fmt.Errorf("channels: eski değer cache'i silinemedi: %w", err)
	}
	if _, err := tx.Exec(ctx,
		`DELETE FROM channels.external_category_attributes
		 WHERE marketplace_code = $1 AND external_category_id = $2`,
		marketplaceCode, externalCategoryID); err != nil {
		return fmt.Errorf("channels: eski özellik cache'i silinemedi: %w", err)
	}

	for _, node := range nodes {
		if _, err := tx.Exec(ctx,
			`INSERT INTO channels.external_category_attributes
			   (id, marketplace_code, external_category_id, external_attribute_id, name,
			    required, allow_custom, is_variant, is_slicer, synced_at)
			 VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10)`,
			uuid.New(), marketplaceCode, externalCategoryID, node.ExternalAttributeID, node.Name,
			node.Required, node.AllowCustom, node.IsVariant, node.IsSlicer, syncedAt); err != nil {
			return fmt.Errorf("channels: özellik cache'i yazılamadı: %w", err)
		}
		for _, value := range node.Values {
			if _, err := tx.Exec(ctx,
				`INSERT INTO channels.external_attribute_values
				   (id, marketplace_code, external_category_id, external_attribute_id,
				    external_value_id, name, synced_at)
				 VALUES ($1, $2, $3, $4, $5, $6, $7)`,
				uuid.New(), marketplaceCode, externalCategoryID, node.ExternalAttributeID,
				value.ExternalValueID, value.Name, syncedAt); err != nil {
				return fmt.Errorf("channels: değer cache'i yazılamadı: %w", err)
			}
		}
	}
	return tx.Commit(ctx)
}

// --- İmport işleri ---

const importRunColumns = `id, tenant_id, marketplace_code, status, created_at, started_at,
	completed_at, total_products, processed_products, imported_products, skipped_products,
	failed_products, error_message`

// scanImportRun, tek iş satırını okur (hatalar ayrı yüklenir).
func scanImportRun(row pgx.Row) (*domain.ProductImportRun, error) {
	var run domain.ProductImportRun
	var status string
	err := row.Scan(&run.ID, &run.TenantID, &run.MarketplaceCode, &status, &run.CreatedAt,
		&run.StartedAt, &run.CompletedAt, &run.TotalProducts, &run.ProcessedProducts,
		&run.ImportedProducts, &run.SkippedProducts, &run.FailedProducts, &run.ErrorMessage)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("channels: import işi okunamadı: %w", err)
	}
	run.Status = domain.ProductImportStatus(status)
	return &run, nil
}

// loadImportErrors, işin hata kayıtlarını yükler.
func (r *Repository) loadImportErrors(ctx context.Context, run *domain.ProductImportRun) error {
	rows, err := r.pool.Query(ctx,
		`SELECT id, product_main_id, barcode, message
		 FROM channels.product_import_run_errors WHERE product_import_run_id = $1`, run.ID)
	if err != nil {
		return fmt.Errorf("channels: import hataları okunamadı: %w", err)
	}
	defer rows.Close()
	for rows.Next() {
		var e domain.ProductImportError
		if err := rows.Scan(&e.ID, &e.ProductMainID, &e.Barcode, &e.Message); err != nil {
			return err
		}
		run.Errors = append(run.Errors, e)
	}
	return rows.Err()
}

// GetActiveImportRun, tenant'ın pending/running import işini döner; yoksa nil.
func (r *Repository) GetActiveImportRun(ctx context.Context, tenantID uuid.UUID, marketplaceCode string) (*domain.ProductImportRun, error) {
	return scanImportRun(r.pool.QueryRow(ctx,
		`SELECT `+importRunColumns+` FROM channels.product_import_runs
		 WHERE tenant_id = $1 AND marketplace_code = $2 AND status IN ('pending', 'running')
		 ORDER BY created_at DESC LIMIT 1`, tenantID, marketplaceCode))
}

// GetImportRun, kimlikle import işini hatalarıyla döner; yoksa nil.
func (r *Repository) GetImportRun(ctx context.Context, tenantID uuid.UUID, id uuid.UUID) (*domain.ProductImportRun, error) {
	run, err := scanImportRun(r.pool.QueryRow(ctx,
		`SELECT `+importRunColumns+` FROM channels.product_import_runs
		 WHERE tenant_id = $1 AND id = $2`, tenantID, id))
	if err != nil || run == nil {
		return run, err
	}
	if err := r.loadImportErrors(ctx, run); err != nil {
		return nil, err
	}
	return run, nil
}

// ListRecentImportRuns, son işleri yeniden eskiye döner.
func (r *Repository) ListRecentImportRuns(ctx context.Context, tenantID uuid.UUID, marketplaceCode string, limit int) ([]*domain.ProductImportRun, error) {
	rows, err := r.pool.Query(ctx,
		`SELECT `+importRunColumns+` FROM channels.product_import_runs
		 WHERE tenant_id = $1 AND marketplace_code = $2
		 ORDER BY created_at DESC LIMIT $3`, tenantID, marketplaceCode, limit)
	if err != nil {
		return nil, fmt.Errorf("channels: import işleri listelenemedi: %w", err)
	}
	defer rows.Close()

	items := []*domain.ProductImportRun{}
	for rows.Next() {
		run, err := scanImportRun(rows)
		if err != nil {
			return nil, err
		}
		items = append(items, run)
	}
	return items, rows.Err()
}

// AddImportRun, yeni import işini ekler.
func (r *Repository) AddImportRun(ctx context.Context, run *domain.ProductImportRun) error {
	_, err := r.pool.Exec(ctx,
		`INSERT INTO channels.product_import_runs
		   (id, tenant_id, marketplace_code, status, created_at, started_at, completed_at,
		    total_products, processed_products, imported_products, skipped_products,
		    failed_products, error_message)
		 VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,$12,$13)`,
		run.ID, run.TenantID, run.MarketplaceCode, string(run.Status), run.CreatedAt,
		run.StartedAt, run.CompletedAt, run.TotalProducts, run.ProcessedProducts,
		run.ImportedProducts, run.SkippedProducts, run.FailedProducts, run.ErrorMessage)
	if err != nil {
		return fmt.Errorf("channels: import işi eklenemedi: %w", err)
	}
	return nil
}

// --- Yayın işleri ---

const publicationRunColumns = `id, tenant_id, marketplace_code, status, created_at, started_at,
	completed_at, total_items, processed_items, published_items, failed_items, error_message`

// scanPublicationRun, tek iş satırını okur.
func scanPublicationRun(row pgx.Row) (*domain.ProductPublicationRun, error) {
	var run domain.ProductPublicationRun
	var status string
	err := row.Scan(&run.ID, &run.TenantID, &run.MarketplaceCode, &status, &run.CreatedAt,
		&run.StartedAt, &run.CompletedAt, &run.TotalItems, &run.ProcessedItems,
		&run.PublishedItems, &run.FailedItems, &run.ErrorMessage)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("channels: yayın işi okunamadı: %w", err)
	}
	run.Status = domain.PublicationStatus(status)
	return &run, nil
}

// GetActivePublicationRun, tenant'ın pending/running yayın işini döner; yoksa nil.
func (r *Repository) GetActivePublicationRun(ctx context.Context, tenantID uuid.UUID, marketplaceCode string) (*domain.ProductPublicationRun, error) {
	return scanPublicationRun(r.pool.QueryRow(ctx,
		`SELECT `+publicationRunColumns+` FROM channels.product_publication_runs
		 WHERE tenant_id = $1 AND marketplace_code = $2 AND status IN ('pending', 'running')
		 ORDER BY created_at DESC LIMIT 1`, tenantID, marketplaceCode))
}

// GetPublicationRun, kimlikle yayın işini hatalarıyla döner; yoksa nil.
func (r *Repository) GetPublicationRun(ctx context.Context, tenantID uuid.UUID, id uuid.UUID) (*domain.ProductPublicationRun, error) {
	run, err := scanPublicationRun(r.pool.QueryRow(ctx,
		`SELECT `+publicationRunColumns+` FROM channels.product_publication_runs
		 WHERE tenant_id = $1 AND id = $2`, tenantID, id))
	if err != nil || run == nil {
		return run, err
	}
	rows, err := r.pool.Query(ctx,
		`SELECT id, product_item_id, message
		 FROM channels.product_publication_run_errors WHERE product_publication_run_id = $1`, run.ID)
	if err != nil {
		return nil, fmt.Errorf("channels: yayın hataları okunamadı: %w", err)
	}
	defer rows.Close()
	for rows.Next() {
		var e domain.ProductPublicationError
		if err := rows.Scan(&e.ID, &e.ProductItemID, &e.Message); err != nil {
			return nil, err
		}
		run.Errors = append(run.Errors, e)
	}
	return run, rows.Err()
}

// AddPublicationRun, yeni yayın işini ekler.
func (r *Repository) AddPublicationRun(ctx context.Context, run *domain.ProductPublicationRun) error {
	_, err := r.pool.Exec(ctx,
		`INSERT INTO channels.product_publication_runs
		   (id, tenant_id, marketplace_code, status, created_at, started_at, completed_at,
		    total_items, processed_items, published_items, failed_items, error_message)
		 VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,$12)`,
		run.ID, run.TenantID, run.MarketplaceCode, string(run.Status), run.CreatedAt,
		run.StartedAt, run.CompletedAt, run.TotalItems, run.ProcessedItems,
		run.PublishedItems, run.FailedItems, run.ErrorMessage)
	if err != nil {
		return fmt.Errorf("channels: yayın işi eklenemedi: %w", err)
	}
	return nil
}

// --- Kategori eşlemeleri ---

// GetCategoryMapping, tenant'ın kategori eşlemesini döner; yoksa nil.
func (r *Repository) GetCategoryMapping(ctx context.Context, tenantID uuid.UUID, marketplaceCode string, catalogCategoryID uuid.UUID) (*domain.CategoryChannelMapping, error) {
	var m domain.CategoryChannelMapping
	err := r.pool.QueryRow(ctx,
		`SELECT id, catalog_category_id, marketplace_code, external_id
		 FROM channels.category_channel_mappings
		 WHERE tenant_id = $1 AND marketplace_code = $2 AND catalog_category_id = $3`,
		tenantID, marketplaceCode, catalogCategoryID).
		Scan(&m.ID, &m.CatalogCategoryID, &m.MarketplaceCode, &m.ExternalID)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("channels: kategori eşlemesi okunamadı: %w", err)
	}
	return &m, nil
}

// ResolveExternalCategoryID, eşlenen harici kategori kimliğini döner; yoksa nil.
func (r *Repository) ResolveExternalCategoryID(ctx context.Context, tenantID uuid.UUID, marketplaceCode string, catalogCategoryID uuid.UUID) (*string, error) {
	mapping, err := r.GetCategoryMapping(ctx, tenantID, marketplaceCode, catalogCategoryID)
	if err != nil || mapping == nil {
		return nil, err
	}
	return &mapping.ExternalID, nil
}

// ListCategoryMappings, eşlemeleri sayfalanmış döner ve toplam sayıyı verir.
// ListMappedCategoryIDs, tenant'ın bir pazaryerinde eşlediği tüm catalog
// kategori kimliklerini döner (.NET ProcessPublicationHandler.
// ListMappedCategoryIdsAsync'in sayfalama olmadan tek sorguya indirgenmiş
// karşılığı — worker içi kullanım, kablo sözleşmesi değil).
func (r *Repository) ListMappedCategoryIDs(ctx context.Context, tenantID uuid.UUID, marketplaceCode string) ([]uuid.UUID, error) {
	rows, err := r.pool.Query(ctx,
		`SELECT DISTINCT catalog_category_id FROM channels.category_channel_mappings
		 WHERE tenant_id = $1 AND marketplace_code = $2`, tenantID, marketplaceCode)
	if err != nil {
		return nil, fmt.Errorf("channels: eşlenmiş kategoriler okunamadı: %w", err)
	}
	defer rows.Close()

	ids := []uuid.UUID{}
	for rows.Next() {
		var id uuid.UUID
		if err := rows.Scan(&id); err != nil {
			return nil, err
		}
		ids = append(ids, id)
	}
	return ids, rows.Err()
}

func (r *Repository) ListCategoryMappings(ctx context.Context, tenantID uuid.UUID, marketplaceCode string, catalogCategoryID *uuid.UUID, p sharedkernel.Pagination) ([]*domain.CategoryChannelMapping, int, error) {
	where := `WHERE tenant_id = $1 AND marketplace_code = $2`
	args := []any{tenantID, marketplaceCode}
	if catalogCategoryID != nil {
		where += ` AND catalog_category_id = $3`
		args = append(args, *catalogCategoryID)
	}

	var total int
	if err := r.pool.QueryRow(ctx,
		`SELECT count(*) FROM channels.category_channel_mappings `+where, args...).Scan(&total); err != nil {
		return nil, 0, fmt.Errorf("channels: kategori eşlemeleri sayılamadı: %w", err)
	}

	rows, err := r.pool.Query(ctx,
		`SELECT id, catalog_category_id, marketplace_code, external_id
		 FROM channels.category_channel_mappings `+where+
			fmt.Sprintf(` ORDER BY external_id OFFSET %d LIMIT %d`, p.Skip(), p.PageSize), args...)
	if err != nil {
		return nil, 0, fmt.Errorf("channels: kategori eşlemeleri listelenemedi: %w", err)
	}
	defer rows.Close()

	items := []*domain.CategoryChannelMapping{}
	for rows.Next() {
		var m domain.CategoryChannelMapping
		if err := rows.Scan(&m.ID, &m.CatalogCategoryID, &m.MarketplaceCode, &m.ExternalID); err != nil {
			return nil, 0, err
		}
		items = append(items, &m)
	}
	return items, total, rows.Err()
}

// AddCategoryMapping, yeni eşlemeyi ekler.
func (r *Repository) AddCategoryMapping(ctx context.Context, tenantID uuid.UUID, m *domain.CategoryChannelMapping) error {
	_, err := r.pool.Exec(ctx,
		`INSERT INTO channels.category_channel_mappings
		   (id, tenant_id, catalog_category_id, marketplace_code, external_id)
		 VALUES ($1, $2, $3, $4, $5)`,
		m.ID, tenantID, m.CatalogCategoryID, m.MarketplaceCode, m.ExternalID)
	if err != nil {
		return fmt.Errorf("channels: kategori eşlemesi eklenemedi: %w", err)
	}
	return nil
}

// UpdateCategoryMapping, eşlemeyi kalıcılaştırır.
func (r *Repository) UpdateCategoryMapping(ctx context.Context, tenantID uuid.UUID, m *domain.CategoryChannelMapping) error {
	_, err := r.pool.Exec(ctx,
		`UPDATE channels.category_channel_mappings SET external_id = $3
		 WHERE tenant_id = $1 AND id = $2`, tenantID, m.ID, m.ExternalID)
	if err != nil {
		return fmt.Errorf("channels: kategori eşlemesi güncellenemedi: %w", err)
	}
	return nil
}

// RemoveCategoryMapping, eşlemeyi siler.
func (r *Repository) RemoveCategoryMapping(ctx context.Context, tenantID uuid.UUID, id uuid.UUID) error {
	_, err := r.pool.Exec(ctx,
		`DELETE FROM channels.category_channel_mappings WHERE tenant_id = $1 AND id = $2`,
		tenantID, id)
	if err != nil {
		return fmt.Errorf("channels: kategori eşlemesi silinemedi: %w", err)
	}
	return nil
}

// --- Alan eşlemeleri ---

const attributeMappingColumns = `id, marketplace_code, catalog_category_id, source_type,
	catalog_source_id, external_attribute_id`

// scanAttributeMapping, tek eşleme satırını okur.
func scanAttributeMapping(row pgx.Row) (*domain.AttributeChannelMapping, error) {
	var m domain.AttributeChannelMapping
	var sourceType string
	err := row.Scan(&m.ID, &m.MarketplaceCode, &m.CatalogCategoryID, &sourceType,
		&m.CatalogSourceID, &m.ExternalAttributeID)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("channels: alan eşlemesi okunamadı: %w", err)
	}
	m.SourceType = domain.AttributeMappingSourceType(sourceType)
	return &m, nil
}

// GetAttributeMappingByID, kimlikle alan eşlemesini döner; yoksa nil.
func (r *Repository) GetAttributeMappingByID(ctx context.Context, tenantID uuid.UUID, id uuid.UUID) (*domain.AttributeChannelMapping, error) {
	return scanAttributeMapping(r.pool.QueryRow(ctx,
		`SELECT `+attributeMappingColumns+` FROM channels.attribute_channel_mappings
		 WHERE tenant_id = $1 AND id = $2`, tenantID, id))
}

// GetAttributeMapping, doğal anahtarla alan eşlemesini döner; yoksa nil.
func (r *Repository) GetAttributeMapping(ctx context.Context, tenantID uuid.UUID, marketplaceCode string, catalogCategoryID uuid.UUID, sourceType domain.AttributeMappingSourceType, catalogSourceID uuid.UUID) (*domain.AttributeChannelMapping, error) {
	return scanAttributeMapping(r.pool.QueryRow(ctx,
		`SELECT `+attributeMappingColumns+` FROM channels.attribute_channel_mappings
		 WHERE tenant_id = $1 AND marketplace_code = $2 AND catalog_category_id = $3
		   AND source_type = $4 AND catalog_source_id = $5`,
		tenantID, marketplaceCode, catalogCategoryID, string(sourceType), catalogSourceID))
}

// ListAttributeMappings, kategori altındaki eşlemeleri sayfalanmış döner.
func (r *Repository) ListAttributeMappings(ctx context.Context, tenantID uuid.UUID, marketplaceCode string, catalogCategoryID uuid.UUID, sourceType *domain.AttributeMappingSourceType, p sharedkernel.Pagination) ([]*domain.AttributeChannelMapping, int, error) {
	where := `WHERE tenant_id = $1 AND marketplace_code = $2 AND catalog_category_id = $3`
	args := []any{tenantID, marketplaceCode, catalogCategoryID}
	if sourceType != nil {
		where += ` AND source_type = $4`
		args = append(args, string(*sourceType))
	}

	var total int
	if err := r.pool.QueryRow(ctx,
		`SELECT count(*) FROM channels.attribute_channel_mappings `+where, args...).Scan(&total); err != nil {
		return nil, 0, fmt.Errorf("channels: alan eşlemeleri sayılamadı: %w", err)
	}

	rows, err := r.pool.Query(ctx,
		`SELECT `+attributeMappingColumns+` FROM channels.attribute_channel_mappings `+where+
			fmt.Sprintf(` ORDER BY external_attribute_id OFFSET %d LIMIT %d`, p.Skip(), p.PageSize), args...)
	if err != nil {
		return nil, 0, fmt.Errorf("channels: alan eşlemeleri listelenemedi: %w", err)
	}
	defer rows.Close()

	items := []*domain.AttributeChannelMapping{}
	for rows.Next() {
		mapping, err := scanAttributeMapping(rows)
		if err != nil {
			return nil, 0, err
		}
		items = append(items, mapping)
	}
	return items, total, rows.Err()
}

// AddAttributeMapping, yeni alan eşlemesini ekler.
func (r *Repository) AddAttributeMapping(ctx context.Context, tenantID uuid.UUID, m *domain.AttributeChannelMapping) error {
	_, err := r.pool.Exec(ctx,
		`INSERT INTO channels.attribute_channel_mappings
		   (id, tenant_id, marketplace_code, catalog_category_id, source_type,
		    catalog_source_id, external_attribute_id)
		 VALUES ($1,$2,$3,$4,$5,$6,$7)`,
		m.ID, tenantID, m.MarketplaceCode, m.CatalogCategoryID, string(m.SourceType),
		m.CatalogSourceID, m.ExternalAttributeID)
	if err != nil {
		return fmt.Errorf("channels: alan eşlemesi eklenemedi: %w", err)
	}
	return nil
}

// UpdateAttributeMapping, alan eşlemesini kalıcılaştırır.
func (r *Repository) UpdateAttributeMapping(ctx context.Context, tenantID uuid.UUID, m *domain.AttributeChannelMapping) error {
	_, err := r.pool.Exec(ctx,
		`UPDATE channels.attribute_channel_mappings SET external_attribute_id = $3
		 WHERE tenant_id = $1 AND id = $2`, tenantID, m.ID, m.ExternalAttributeID)
	if err != nil {
		return fmt.Errorf("channels: alan eşlemesi güncellenemedi: %w", err)
	}
	return nil
}

// RemoveAttributeMapping, alan eşlemesini (altındaki değer eşlemeleriyle) siler.
func (r *Repository) RemoveAttributeMapping(ctx context.Context, tenantID uuid.UUID, id uuid.UUID) error {
	tx, err := r.pool.Begin(ctx)
	if err != nil {
		return fmt.Errorf("channels: alan eşlemesi silme işlemi başlatılamadı: %w", err)
	}
	defer func() { _ = tx.Rollback(ctx) }()

	if _, err := tx.Exec(ctx,
		`DELETE FROM channels.attribute_value_channel_mappings
		 WHERE tenant_id = $1 AND attribute_channel_mapping_id = $2`, tenantID, id); err != nil {
		return fmt.Errorf("channels: değer eşlemeleri silinemedi: %w", err)
	}
	if _, err := tx.Exec(ctx,
		`DELETE FROM channels.attribute_channel_mappings WHERE tenant_id = $1 AND id = $2`,
		tenantID, id); err != nil {
		return fmt.Errorf("channels: alan eşlemesi silinemedi: %w", err)
	}
	return tx.Commit(ctx)
}

// --- Değer eşlemeleri ---

// GetValueMapping, doğal anahtarla değer eşlemesini döner; yoksa nil.
func (r *Repository) GetValueMapping(ctx context.Context, tenantID uuid.UUID, attributeMappingID, catalogValueID uuid.UUID) (*domain.AttributeValueChannelMapping, error) {
	var m domain.AttributeValueChannelMapping
	err := r.pool.QueryRow(ctx,
		`SELECT id, attribute_channel_mapping_id, catalog_value_id, external_value_id
		 FROM channels.attribute_value_channel_mappings
		 WHERE tenant_id = $1 AND attribute_channel_mapping_id = $2 AND catalog_value_id = $3`,
		tenantID, attributeMappingID, catalogValueID).
		Scan(&m.ID, &m.AttributeChannelMappingID, &m.CatalogValueID, &m.ExternalValueID)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("channels: değer eşlemesi okunamadı: %w", err)
	}
	return &m, nil
}

// ListValueMappings, alan eşlemesi altındaki değer eşlemelerini döner.
func (r *Repository) ListValueMappings(ctx context.Context, tenantID uuid.UUID, attributeMappingID uuid.UUID) ([]*domain.AttributeValueChannelMapping, error) {
	rows, err := r.pool.Query(ctx,
		`SELECT id, attribute_channel_mapping_id, catalog_value_id, external_value_id
		 FROM channels.attribute_value_channel_mappings
		 WHERE tenant_id = $1 AND attribute_channel_mapping_id = $2
		 ORDER BY external_value_id`, tenantID, attributeMappingID)
	if err != nil {
		return nil, fmt.Errorf("channels: değer eşlemeleri listelenemedi: %w", err)
	}
	defer rows.Close()

	items := []*domain.AttributeValueChannelMapping{}
	for rows.Next() {
		var m domain.AttributeValueChannelMapping
		if err := rows.Scan(&m.ID, &m.AttributeChannelMappingID, &m.CatalogValueID, &m.ExternalValueID); err != nil {
			return nil, err
		}
		items = append(items, &m)
	}
	return items, rows.Err()
}

// AddValueMapping, yeni değer eşlemesini ekler.
func (r *Repository) AddValueMapping(ctx context.Context, tenantID uuid.UUID, m *domain.AttributeValueChannelMapping) error {
	_, err := r.pool.Exec(ctx,
		`INSERT INTO channels.attribute_value_channel_mappings
		   (id, tenant_id, attribute_channel_mapping_id, catalog_value_id, external_value_id)
		 VALUES ($1, $2, $3, $4, $5)`,
		m.ID, tenantID, m.AttributeChannelMappingID, m.CatalogValueID, m.ExternalValueID)
	if err != nil {
		return fmt.Errorf("channels: değer eşlemesi eklenemedi: %w", err)
	}
	return nil
}

// UpdateValueMapping, değer eşlemesini kalıcılaştırır.
func (r *Repository) UpdateValueMapping(ctx context.Context, tenantID uuid.UUID, m *domain.AttributeValueChannelMapping) error {
	_, err := r.pool.Exec(ctx,
		`UPDATE channels.attribute_value_channel_mappings SET external_value_id = $3
		 WHERE tenant_id = $1 AND id = $2`, tenantID, m.ID, m.ExternalValueID)
	if err != nil {
		return fmt.Errorf("channels: değer eşlemesi güncellenemedi: %w", err)
	}
	return nil
}
