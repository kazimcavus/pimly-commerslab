// Package outbox, işlemsel outbox deseninin ortak mekanizmasını içerir
// (.NET Pimly.Outbox karşılığı). Her modül şeması kendi outbox_messages
// tablosunu taşır (catalog, pricing, inventory); yazma, aggregate değişikliğiyle
// AYNI transaction'da yapılır ki olay ile veri asla ayrışmasın.
//
// Olay tip anahtarları: .NET satırları CLR FullName taşır
// ("Catalog.Domain.Products.Events.ProductItemCreated"); Go, sürümlenebilir
// kararlı adlar yazar ("catalog.product_item_created.v1"). Dispatcher her iki
// adı da tanır (LegacyNames eşlemesi) — böylece cutover sırasında bekleyen eski
// satırlar da işlenebilir.
package outbox

import (
	"context"
	"encoding/json"
	"fmt"
	"time"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"
)

// Kararlı olay adları. Yeni olay eklerken buraya bir sabit ve LegacyNames'e
// (varsa) .NET karşılığı eklenmelidir.
const (
	// EventProductItemCreated: yeni satılabilir kalem oluşturuldu (Catalog yayar).
	EventProductItemCreated = "catalog.product_item_created.v1"

	// EventProductItemDeleted: satılabilir kalem silindi; Pricing/Inventory
	// kayıtlarını temizler, Channels listing'i işaretler.
	EventProductItemDeleted = "catalog.product_item_deleted.v1"

	// EventProductContentChanged: ürünün pazaryerine giden içeriği değişti;
	// Channels ilgili listing'leri content-dirty işaretler.
	EventProductContentChanged = "catalog.product_content_changed.v1"
)

// LegacyNames, .NET CLR FullName → kararlı ad eşlemesidir; dispatcher eski
// satırları bu tabloyla çözer.
var LegacyNames = map[string]string{
	"Catalog.Domain.Products.Events.ProductItemCreated":    EventProductItemCreated,
	"Catalog.Domain.Products.Events.ProductItemDeleted":    EventProductItemDeleted,
	"Catalog.Domain.Products.Events.ProductContentChanged": EventProductContentChanged,
}

// Event, outbox'a yazılabilir bir bütünleşme olayıdır. Name kararlı olay adını
// döner; payload gövdesi olayın kendisidir (snake_case JSON etiketleriyle).
type Event interface {
	// EventName, olayın kararlı adını döner.
	EventName() string
}

// ProductItemCreated, yeni satılabilir kalem olayıdır
// (.NET Catalog.Domain.Products.Events.ProductItemCreated payload'ıyla uyumlu).
type ProductItemCreated struct {
	ProductItemID uuid.UUID `json:"product_item_id"`
	ProductID     uuid.UUID `json:"product_id"`
	OccurredOnUtc time.Time `json:"occurred_on_utc"`
}

// EventName, kararlı olay adını döner.
func (ProductItemCreated) EventName() string { return EventProductItemCreated }

// ProductItemDeleted, kalem silme olayıdır.
type ProductItemDeleted struct {
	ProductItemID uuid.UUID `json:"product_item_id"`
	ProductID     uuid.UUID `json:"product_id"`
	OccurredOnUtc time.Time `json:"occurred_on_utc"`
}

// EventName, kararlı olay adını döner.
func (ProductItemDeleted) EventName() string { return EventProductItemDeleted }

// ProductContentChanged, ürün içeriği değişikliği olayıdır.
type ProductContentChanged struct {
	ProductID      uuid.UUID   `json:"product_id"`
	ProductItemIds []uuid.UUID `json:"product_item_ids"`
	OccurredOnUtc  time.Time   `json:"occurred_on_utc"`
}

// EventName, kararlı olay adını döner.
func (ProductContentChanged) EventName() string { return EventProductContentChanged }

// Write, olayları verilen şemanın outbox_messages tablosuna, çağıranın
// transaction'ı içinde yazar (.NET OutboxWriter.WriteOutboxMessages karşılığı).
// tenantID boş olamaz — bütünleşme olayı tenant'sız var olamaz.
func Write(ctx context.Context, tx pgx.Tx, schema string, tenantID uuid.UUID, events []Event) error {
	if len(events) == 0 {
		return nil
	}
	if tenantID == uuid.Nil {
		return fmt.Errorf("outbox: bütünleşme olayı tenant'sız yazılamaz")
	}
	for _, event := range events {
		payload, err := json.Marshal(event)
		if err != nil {
			return fmt.Errorf("outbox: olay serileştirilemedi (%s): %w", event.EventName(), err)
		}
		if _, err := tx.Exec(ctx,
			`INSERT INTO `+schema+`.outbox_messages
			   (id, tenant_id, type, payload, occurred_on_utc, processed_on_utc, attempts, error)
			 VALUES ($1, $2, $3, $4, $5, NULL, 0, NULL)`,
			uuid.New(), tenantID, event.EventName(), payload, time.Now().UTC()); err != nil {
			return fmt.Errorf("outbox: olay yazılamadı (%s): %w", event.EventName(), err)
		}
	}
	return nil
}
