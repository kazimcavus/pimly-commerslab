package infrastructure

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgxpool"

	"pimly.commerslab/backend-go/internal/modules/catalog/domain/skugen"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// SkuConfigRepository, catalog.sku_generator_config tablosunun pgx uygulamasıdır.
// Satır tenant başına tekildir (PK: tenant_id + id=1). segments jsonb belgesi
// .NET'in camelCase biçimiyle uyumlu yazılır ve türetilmiş
// isCounterSegment/isVariantSegment alanlarını da içerir (EF public getter'ları
// serileştirir; bayt uyumu için korunur).
type SkuConfigRepository struct {
	pool *pgxpool.Pool
}

// NewSkuConfigRepository, verilen havuzla yapılandırma deposunu oluşturur.
func NewSkuConfigRepository(pool *pgxpool.Pool) *SkuConfigRepository {
	return &SkuConfigRepository{pool: pool}
}

// dbSegment, segments jsonb belgesindeki tek segmentin veritabanı biçimidir.
type dbSegment struct {
	Type             string  `json:"type"`
	Label            *string `json:"label"`
	Value            *string `json:"value"`
	Start            *int    `json:"start"`
	Width            *int    `json:"width"`
	Digits           *int    `json:"digits"`
	Source           *string `json:"source"`
	IsCounterSegment bool    `json:"isCounterSegment"`
	IsVariantSegment bool    `json:"isVariantSegment"`
}

// marshalSegments, segmentleri veritabanı jsonb biçimine çevirir.
func marshalSegments(segments []skugen.Segment) ([]byte, error) {
	rows := make([]dbSegment, len(segments))
	for i, s := range segments {
		rows[i] = dbSegment{Type: s.Type, Label: s.Label, Value: s.Value, Start: s.Start,
			Width: s.Width, Digits: s.Digits, Source: s.Source,
			IsCounterSegment: s.IsCounterSegment(), IsVariantSegment: s.IsVariantSegment()}
	}
	return json.Marshal(rows)
}

// unmarshalSegments, veritabanı jsonb belgesini domain segmentlerine çevirir.
func unmarshalSegments(data []byte) ([]skugen.Segment, error) {
	var rows []dbSegment
	if err := json.Unmarshal(data, &rows); err != nil {
		return nil, err
	}
	segments := make([]skugen.Segment, len(rows))
	for i, row := range rows {
		segments[i] = skugen.Segment{Type: row.Type, Label: row.Label, Value: row.Value,
			Start: row.Start, Width: row.Width, Digits: row.Digits, Source: row.Source}
	}
	return segments, nil
}

// Get, tenant'ın yapılandırmasını döner; yoksa nil.
func (r *SkuConfigRepository) Get(ctx context.Context, tenantID uuid.UUID) (*skugen.Config, error) {
	var enabled bool
	var counterNext int64
	var segmentsJSON []byte
	err := r.pool.QueryRow(ctx,
		`SELECT enabled, counter_next_value, segments FROM catalog.sku_generator_config
		 WHERE tenant_id = $1 AND id = $2`, tenantID, skugen.SingletonID).
		Scan(&enabled, &counterNext, &segmentsJSON)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("catalog: SKU yapılandırması okunamadı: %w", err)
	}
	segments, err := unmarshalSegments(segmentsJSON)
	if err != nil {
		return nil, fmt.Errorf("catalog: SKU segmentleri çözümlenemedi: %w", err)
	}
	return &skugen.Config{Enabled: enabled, Segments: segments, CounterNextValue: counterNext}, nil
}

// Add, yeni yapılandırma satırını ekler.
func (r *SkuConfigRepository) Add(ctx context.Context, tenantID uuid.UUID, config *skugen.Config) error {
	segments, err := marshalSegments(config.Segments)
	if err != nil {
		return fmt.Errorf("catalog: SKU segmentleri serileştirilemedi: %w", err)
	}
	if _, err := r.pool.Exec(ctx,
		`INSERT INTO catalog.sku_generator_config (id, enabled, counter_next_value, segments, tenant_id)
		 VALUES ($1, $2, $3, $4, $5)`,
		skugen.SingletonID, config.Enabled, config.CounterNextValue, segments, tenantID); err != nil {
		return fmt.Errorf("catalog: SKU yapılandırması eklenemedi: %w", err)
	}
	return nil
}

// Update, yapılandırmayı kalıcılaştırır.
func (r *SkuConfigRepository) Update(ctx context.Context, tenantID uuid.UUID, config *skugen.Config) error {
	segments, err := marshalSegments(config.Segments)
	if err != nil {
		return fmt.Errorf("catalog: SKU segmentleri serileştirilemedi: %w", err)
	}
	if _, err := r.pool.Exec(ctx,
		`UPDATE catalog.sku_generator_config SET enabled = $3, counter_next_value = $4, segments = $5
		 WHERE tenant_id = $1 AND id = $2`,
		tenantID, skugen.SingletonID, config.Enabled, config.CounterNextValue, segments); err != nil {
		return fmt.Errorf("catalog: SKU yapılandırması güncellenemedi: %w", err)
	}
	return nil
}

// SkuCounterAllocator, Postgres üzerinde atomik sayaç bloğu rezervasyonu yapar
// (.NET SkuCounterAllocator portu): tek UPDATE ... RETURNING ile count kadar
// değer ayrılır ve bloğun başlangıcı döner.
type SkuCounterAllocator struct {
	pool *pgxpool.Pool
}

// NewSkuCounterAllocator, verilen havuzla ayırıcıyı oluşturur.
func NewSkuCounterAllocator(pool *pgxpool.Pool) *SkuCounterAllocator {
	return &SkuCounterAllocator{pool: pool}
}

// Reserve, count kadar sayaç değerini atomik ayırır.
func (a *SkuCounterAllocator) Reserve(ctx context.Context, tenantID uuid.UUID, count int) (int64, *sharedkernel.Error) {
	if count < 1 {
		return 0, sharedkernel.NewValidationError("Count must be at least 1.")
	}
	var start int64
	err := a.pool.QueryRow(ctx,
		`UPDATE catalog.sku_generator_config
		 SET counter_next_value = counter_next_value + $3
		 WHERE tenant_id = $1 AND id = $2
		 RETURNING (counter_next_value - $3)::bigint`,
		tenantID, skugen.SingletonID, count).Scan(&start)
	if errors.Is(err, pgx.ErrNoRows) {
		return 0, sharedkernel.NewNotFoundError("SKU generator is not configured.")
	}
	if err != nil {
		return 0, sharedkernel.NewInternalError(err.Error())
	}
	return start, nil
}
