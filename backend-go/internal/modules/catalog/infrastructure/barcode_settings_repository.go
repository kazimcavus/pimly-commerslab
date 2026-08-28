package infrastructure

import (
	"context"
	"errors"
	"fmt"
	"strconv"
	"time"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgxpool"

	"pimly.commerslab/backend-go/internal/modules/catalog/application"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// BarcodeRepository, catalog.barcode_sequence + catalog.barcode_allocations
// tablolarının pgx uygulamasıdır. Tahsis, tek UPDATE..RETURNING ile atomiktir
// (.NET BarcodeAllocator ile aynı SQL deseni).
type BarcodeRepository struct {
	pool *pgxpool.Pool
}

// NewBarcodeRepository, verilen havuzla barkod deposunu oluşturur.
func NewBarcodeRepository(pool *pgxpool.Pool) *BarcodeRepository {
	return &BarcodeRepository{pool: pool}
}

// GetSequence, tenant'ın serisini döner; yoksa nil.
func (r *BarcodeRepository) GetSequence(ctx context.Context, tenantID uuid.UUID) (*application.BarcodeSequence, error) {
	var s application.BarcodeSequence
	err := r.pool.QueryRow(ctx,
		`SELECT next_value, client_allocation_required FROM catalog.barcode_sequence
		 WHERE tenant_id = $1 AND id = $2`, tenantID, application.BarcodeSequenceSingletonID).
		Scan(&s.NextValue, &s.ClientAllocationRequired)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("catalog: barkod serisi okunamadı: %w", err)
	}
	return &s, nil
}

// AddSequence, başlangıç serisini ekler.
func (r *BarcodeRepository) AddSequence(ctx context.Context, tenantID uuid.UUID, s *application.BarcodeSequence) error {
	if _, err := r.pool.Exec(ctx,
		`INSERT INTO catalog.barcode_sequence (id, next_value, client_allocation_required, tenant_id)
		 VALUES ($1, $2, $3, $4)`,
		application.BarcodeSequenceSingletonID, s.NextValue, s.ClientAllocationRequired, tenantID); err != nil {
		return fmt.Errorf("catalog: barkod serisi eklenemedi: %w", err)
	}
	return nil
}

// UpdateSequence, seriyi kalıcılaştırır.
func (r *BarcodeRepository) UpdateSequence(ctx context.Context, tenantID uuid.UUID, s *application.BarcodeSequence) error {
	if _, err := r.pool.Exec(ctx,
		`UPDATE catalog.barcode_sequence SET next_value = $3, client_allocation_required = $4
		 WHERE tenant_id = $1 AND id = $2`,
		tenantID, application.BarcodeSequenceSingletonID, s.NextValue, s.ClientAllocationRequired); err != nil {
		return fmt.Errorf("catalog: barkod serisi güncellenemedi: %w", err)
	}
	return nil
}

// MaxNumericBarcode, tahsis edilmiş en yüksek sayısal barkodu döner; yoksa 0.
// .NET tarafı bunu bellek içinde hesaplar; sonuç aynıdır (yalnızca rakamlardan
// oluşan değerler sayıya çevrilip en büyüğü alınır).
func (r *BarcodeRepository) MaxNumericBarcode(ctx context.Context, tenantID uuid.UUID) (int64, error) {
	rows, err := r.pool.Query(ctx,
		`SELECT barcode FROM catalog.barcode_allocations WHERE tenant_id = $1`, tenantID)
	if err != nil {
		return 0, fmt.Errorf("catalog: barkod tahsisleri okunamadı: %w", err)
	}
	defer rows.Close()

	var maxValue int64
	for rows.Next() {
		var barcode string
		if err := rows.Scan(&barcode); err != nil {
			return 0, err
		}
		if value, err := strconv.ParseInt(barcode, 10, 64); err == nil && value > maxValue {
			maxValue = value
		}
	}
	return maxValue, rows.Err()
}

// Allocate, seriden count kadar değeri atomik ayırır ve tahsis kayıtlarını
// tek transaction'da yazar.
func (r *BarcodeRepository) Allocate(ctx context.Context, tenantID uuid.UUID, count int) ([]application.BarcodeAllocation, *sharedkernel.Error) {
	tx, err := r.pool.Begin(ctx)
	if err != nil {
		return nil, sharedkernel.NewInternalError(err.Error())
	}
	defer func() { _ = tx.Rollback(ctx) }()

	var start int64
	err = tx.QueryRow(ctx,
		`UPDATE catalog.barcode_sequence SET next_value = next_value + $3
		 WHERE tenant_id = $1 AND id = $2
		 RETURNING (next_value - $3)::bigint`,
		tenantID, application.BarcodeSequenceSingletonID, count).Scan(&start)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, sharedkernel.NewNotFoundError("Barcode sequence is not configured.")
	}
	if err != nil {
		return nil, sharedkernel.NewInternalError(err.Error())
	}

	allocations := make([]application.BarcodeAllocation, count)
	now := time.Now().UTC()
	for i := 0; i < count; i++ {
		allocations[i] = application.BarcodeAllocation{
			ID:          uuid.New(),
			Barcode:     strconv.FormatInt(start+int64(i), 10),
			AllocatedAt: now,
		}
		if _, err := tx.Exec(ctx,
			`INSERT INTO catalog.barcode_allocations (id, barcode, allocated_at, tenant_id)
			 VALUES ($1, $2, $3, $4)`,
			allocations[i].ID, allocations[i].Barcode, allocations[i].AllocatedAt, tenantID); err != nil {
			return nil, sharedkernel.NewInternalError(err.Error())
		}
	}
	if err := tx.Commit(ctx); err != nil {
		return nil, sharedkernel.NewInternalError(err.Error())
	}
	return allocations, nil
}

// ListAllocations, tahsisleri en yeniden eskiye (allocated_at, barcode DESC)
// sayfalanmış döner.
func (r *BarcodeRepository) ListAllocations(ctx context.Context, tenantID uuid.UUID, p sharedkernel.Pagination) (sharedkernel.PagedResult[application.BarcodeAllocation], error) {
	var total int
	if err := r.pool.QueryRow(ctx,
		`SELECT count(*) FROM catalog.barcode_allocations WHERE tenant_id = $1`, tenantID).Scan(&total); err != nil {
		return sharedkernel.PagedResult[application.BarcodeAllocation]{}, fmt.Errorf("catalog: tahsisler sayılamadı: %w", err)
	}
	rows, err := r.pool.Query(ctx,
		`SELECT id, barcode, allocated_at FROM catalog.barcode_allocations
		 WHERE tenant_id = $1 ORDER BY allocated_at DESC, barcode DESC OFFSET $2 LIMIT $3`,
		tenantID, p.Skip(), p.PageSize)
	if err != nil {
		return sharedkernel.PagedResult[application.BarcodeAllocation]{}, fmt.Errorf("catalog: tahsisler listelenemedi: %w", err)
	}
	defer rows.Close()

	items := []application.BarcodeAllocation{}
	for rows.Next() {
		var a application.BarcodeAllocation
		if err := rows.Scan(&a.ID, &a.Barcode, &a.AllocatedAt); err != nil {
			return sharedkernel.PagedResult[application.BarcodeAllocation]{}, err
		}
		items = append(items, a)
	}
	if err := rows.Err(); err != nil {
		return sharedkernel.PagedResult[application.BarcodeAllocation]{}, err
	}
	return sharedkernel.NewPagedResult(items, p, total), nil
}

// CatalogSettingsRepository, catalog.catalog_settings tablosunun pgx uygulamasıdır.
type CatalogSettingsRepository struct {
	pool *pgxpool.Pool
}

// NewCatalogSettingsRepository, verilen havuzla ayar deposunu oluşturur.
func NewCatalogSettingsRepository(pool *pgxpool.Pool) *CatalogSettingsRepository {
	return &CatalogSettingsRepository{pool: pool}
}

// catalogSettingsSingletonID, tenant başına tek satırın sabit kimliğidir.
const catalogSettingsSingletonID = 1

// Get, tenant'ın ayarlarını döner; yoksa nil.
func (r *CatalogSettingsRepository) Get(ctx context.Context, tenantID uuid.UUID) (*application.CatalogSettings, error) {
	var s application.CatalogSettings
	err := r.pool.QueryRow(ctx,
		`SELECT slicer_name_position FROM catalog.catalog_settings
		 WHERE tenant_id = $1 AND id = $2`, tenantID, catalogSettingsSingletonID).
		Scan(&s.SlicerNamePosition)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("catalog: ayarlar okunamadı: %w", err)
	}
	return &s, nil
}

// Add, başlangıç ayarlarını ekler.
func (r *CatalogSettingsRepository) Add(ctx context.Context, tenantID uuid.UUID, s *application.CatalogSettings) error {
	if _, err := r.pool.Exec(ctx,
		`INSERT INTO catalog.catalog_settings (id, tenant_id, slicer_name_position)
		 VALUES ($1, $2, $3)`,
		catalogSettingsSingletonID, tenantID, s.SlicerNamePosition); err != nil {
		return fmt.Errorf("catalog: ayarlar eklenemedi: %w", err)
	}
	return nil
}

// Update, ayarları kalıcılaştırır.
func (r *CatalogSettingsRepository) Update(ctx context.Context, tenantID uuid.UUID, s *application.CatalogSettings) error {
	if _, err := r.pool.Exec(ctx,
		`UPDATE catalog.catalog_settings SET slicer_name_position = $3
		 WHERE tenant_id = $1 AND id = $2`,
		tenantID, catalogSettingsSingletonID, s.SlicerNamePosition); err != nil {
		return fmt.Errorf("catalog: ayarlar güncellenemedi: %w", err)
	}
	return nil
}
