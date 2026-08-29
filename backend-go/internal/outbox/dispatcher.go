package outbox

import (
	"context"
	"errors"
	"fmt"
	"log/slog"
	"time"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgxpool"
	"github.com/prometheus/client_golang/prometheus"
	"github.com/prometheus/client_golang/prometheus/promauto"
)

// Handler, tek bir outbox mesajını işler. Mesajın claim'iyle AYNI transaction
// içinde çalışır: başarı → etkiler + processed işareti atomik yazılır; hata →
// her şey geri alınır ve mesaj backoff ile yeniden denenir.
type Handler func(ctx context.Context, tx pgx.Tx, tenantID uuid.UUID, payload []byte) error

// Dispatcher, modül şemalarının outbox_messages tablolarını tarayıp mesajları
// kayıtlı handler'lara dağıtır (.NET OutboxProcessor karşılığı + Go dönemi
// iyileştirmeleri: FOR UPDATE SKIP LOCKED ile çoklu-instance güvenliği, üstel
// backoff ve max-deneme sonrası dead-letter).
type Dispatcher struct {
	pool        *pgxpool.Pool
	schemas     []string
	batchSize   int
	maxAttempts int
	handlers    map[string][]Handler

	processedTotal  *prometheus.CounterVec
	failedTotal     *prometheus.CounterVec
	deadLetterTotal *prometheus.CounterVec
	legacyTotal     *prometheus.CounterVec
}

// NewDispatcher, verilen şemalar ve limitlerle dispatcher oluşturur.
func NewDispatcher(pool *pgxpool.Pool, schemas []string, batchSize, maxAttempts int) *Dispatcher {
	return &Dispatcher{
		pool: pool, schemas: schemas, batchSize: batchSize, maxAttempts: maxAttempts,
		handlers: map[string][]Handler{},
		processedTotal: promauto.NewCounterVec(prometheus.CounterOpts{
			Name: "pimly_outbox_processed_total",
			Help: "Başarıyla işlenen outbox mesajı sayısı."}, []string{"schema", "type"}),
		failedTotal: promauto.NewCounterVec(prometheus.CounterOpts{
			Name: "pimly_outbox_failed_total",
			Help: "İşlenemeyen (yeniden denenecek) outbox mesajı sayısı."}, []string{"schema", "type"}),
		deadLetterTotal: promauto.NewCounterVec(prometheus.CounterOpts{
			Name: "pimly_outbox_dead_letter_total",
			Help: "Azami deneme sayısına ulaşıp dead-letter olan mesaj sayısı."}, []string{"schema", "type"}),
		legacyTotal: promauto.NewCounterVec(prometheus.CounterOpts{
			Name: "pimly_outbox_legacy_name_total",
			Help: "Eski .NET tip adıyla görülen mesaj sayısı (cutover drain görünürlüğü)."}, []string{"schema"}),
	}
}

// Register, kararlı olay adına handler ekler; aynı ada birden çok handler
// bağlanabilir (fan-out) ve HEPSİ başarılı olmadan mesaj işlenmiş sayılmaz.
func (d *Dispatcher) Register(eventName string, handler Handler) {
	d.handlers[eventName] = append(d.handlers[eventName], handler)
}

// resolve, mesaj tip anahtarını kararlı ada çözer: önce doğrudan, sonra eski
// .NET FullName alias tablosundan.
func (d *Dispatcher) resolve(schema, typeName string) (string, bool) {
	if _, ok := d.handlers[typeName]; ok {
		return typeName, true
	}
	if stable, ok := LegacyNames[typeName]; ok {
		d.legacyTotal.WithLabelValues(schema).Inc()
		if _, registered := d.handlers[stable]; registered {
			return stable, true
		}
	}
	return "", false
}

// backoffDelay, deneme sayısına göre bekleme süresini hesaplar: 30s·2ⁿ, tavan 1h.
func backoffDelay(attempts int) time.Duration {
	delay := 30 * time.Second
	for i := 0; i < attempts && delay < time.Hour; i++ {
		delay *= 2
	}
	if delay > time.Hour {
		delay = time.Hour
	}
	return delay
}

// ProcessOnce, tüm şemalarda birer parti işler; herhangi bir mesaj
// işlendiyse true döner (döngü beklemeden devam eder).
func (d *Dispatcher) ProcessOnce(ctx context.Context) (bool, error) {
	anyProcessed := false
	for _, schema := range d.schemas {
		for i := 0; i < d.batchSize; i++ {
			processed, err := d.processNext(ctx, schema)
			if err != nil {
				return anyProcessed, err
			}
			if !processed {
				break
			}
			anyProcessed = true
		}
	}
	return anyProcessed, nil
}

// processNext, şemadan tek bekleyen mesajı claim edip işler; kuyruk boşsa
// false döner. Mesaj kendi transaction'ında işlenir: bir mesajın hatası
// diğerlerini etkilemez ve etkiler processed işaretiyle atomiktir.
func (d *Dispatcher) processNext(ctx context.Context, schema string) (bool, error) {
	tx, err := d.pool.Begin(ctx)
	if err != nil {
		return false, fmt.Errorf("outbox: işlem başlatılamadı: %w", err)
	}
	defer func() { _ = tx.Rollback(ctx) }()

	var (
		id       uuid.UUID
		tenantID uuid.UUID
		typeName string
		payload  []byte
		attempts int
	)
	err = tx.QueryRow(ctx, fmt.Sprintf(`
		SELECT id, tenant_id, type, payload, attempts FROM %s.outbox_messages
		WHERE processed_on_utc IS NULL
		  AND attempts < $1
		  AND (next_attempt_at IS NULL OR next_attempt_at <= now())
		ORDER BY occurred_on_utc
		LIMIT 1
		FOR UPDATE SKIP LOCKED`, schema), d.maxAttempts).
		Scan(&id, &tenantID, &typeName, &payload, &attempts)
	if errors.Is(err, pgx.ErrNoRows) {
		return false, nil
	}
	if err != nil {
		return false, fmt.Errorf("outbox: %s mesajı claim edilemedi: %w", schema, err)
	}

	stableName, ok := d.resolve(schema, typeName)
	if !ok {
		// Tanınmayan tip: yeniden denemek anlamsız — dead-letter'a düşür.
		if uerr := d.markFailed(ctx, tx, schema, id, attempts, d.maxAttempts,
			"unknown event type: "+typeName); uerr != nil {
			return false, uerr
		}
		d.deadLetterTotal.WithLabelValues(schema, typeName).Inc()
		slog.Warn("Outbox message has unknown type; dead-lettered.",
			slog.String("Schema", schema), slog.String("Type", typeName), slog.String("MessageId", id.String()))
		return true, tx.Commit(ctx)
	}

	handlerErr := func() error {
		for _, handler := range d.handlers[stableName] {
			if err := handler(ctx, tx, tenantID, payload); err != nil {
				return err
			}
		}
		return nil
	}()

	if handlerErr != nil {
		// Etkiler geri alınmalı ama attempts/next_attempt_at kalıcı olmalı:
		// bu transaction'ı bırakıp ayrı bir güncellemeyle hatayı işlenir.
		_ = tx.Rollback(ctx)
		newAttempts := attempts + 1
		if err := d.recordFailure(ctx, schema, id, newAttempts, handlerErr.Error()); err != nil {
			return false, err
		}
		if newAttempts >= d.maxAttempts {
			d.deadLetterTotal.WithLabelValues(schema, stableName).Inc()
			slog.Error("Outbox message dead-lettered after max attempts.",
				slog.String("Schema", schema), slog.String("Type", stableName),
				slog.String("MessageId", id.String()), slog.String("Error", handlerErr.Error()))
		} else {
			d.failedTotal.WithLabelValues(schema, stableName).Inc()
			slog.Warn("Outbox message failed; will retry.",
				slog.String("Schema", schema), slog.String("Type", stableName),
				slog.String("MessageId", id.String()), slog.Int("Attempts", newAttempts),
				slog.String("Error", handlerErr.Error()))
		}
		return true, nil
	}

	if _, err := tx.Exec(ctx, fmt.Sprintf(
		`UPDATE %s.outbox_messages SET processed_on_utc = now(), error = NULL WHERE id = $1`, schema), id); err != nil {
		return false, fmt.Errorf("outbox: mesaj işaretlenemedi: %w", err)
	}
	if err := tx.Commit(ctx); err != nil {
		return false, err
	}
	d.processedTotal.WithLabelValues(schema, stableName).Inc()
	return true, nil
}

// markFailed, claim transaction'ı içinde deneme sayacını tavana çeker
// (tanınmayan tip için).
func (d *Dispatcher) markFailed(ctx context.Context, tx pgx.Tx, schema string, id uuid.UUID, _, attempts int, message string) error {
	_, err := tx.Exec(ctx, fmt.Sprintf(
		`UPDATE %s.outbox_messages SET attempts = $2, error = $3 WHERE id = $1`, schema),
		id, attempts, truncateError(message))
	return err
}

// recordFailure, handler hatasını ayrı bir bağlantıda kalıcılaştırır:
// attempts artar, hata metni ve backoff penceresi yazılır.
func (d *Dispatcher) recordFailure(ctx context.Context, schema string, id uuid.UUID, attempts int, message string) error {
	nextAttempt := time.Now().UTC().Add(backoffDelay(attempts))
	_, err := d.pool.Exec(ctx, fmt.Sprintf(
		`UPDATE %s.outbox_messages SET attempts = $2, error = $3, next_attempt_at = $4 WHERE id = $1`, schema),
		id, attempts, truncateError(message), nextAttempt)
	if err != nil {
		return fmt.Errorf("outbox: hata kaydedilemedi: %w", err)
	}
	return nil
}

// truncateError, hata metnini kolon sınırına indirger.
func truncateError(message string) string {
	const maxLength = 2000
	if len(message) > maxLength {
		return message[:maxLength]
	}
	return message
}
