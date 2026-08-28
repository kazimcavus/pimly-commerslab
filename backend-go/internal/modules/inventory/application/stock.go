// Package application, Inventory modülünün kullanım senaryolarını içerir
// (.NET Inventory.Application karşılığı). Kalem başına tek stok kaydı tutulur
// (tek örtük depo); kaleme yalnızca opak product_item_id ile referans verilir.
package application

import (
	"context"
	"time"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// StockLevel, satılabilir kalemin stok miktarıdır (.NET StockLevel aggregate'i).
type StockLevel struct {
	ID            uuid.UUID
	ProductItemID uuid.UUID
	Quantity      int
	UpdatedAt     time.Time
}

// StockLevelDto, stok seviyesinin kablo biçimidir.
type StockLevelDto struct {
	ProductItemID uuid.UUID `json:"product_item_id"`
	Quantity      int       `json:"quantity"`
	UpdatedAt     time.Time `json:"updated_at"`
}

// stockToDto, stok kaydını DTO'ya çevirir.
func stockToDto(s *StockLevel) StockLevelDto {
	return StockLevelDto{ProductItemID: s.ProductItemID, Quantity: s.Quantity, UpdatedAt: s.UpdatedAt}
}

// StockLevelRepository, stok kalıcılık portudur. Add/Update, StockLevelChanged
// olayını AYNI transaction'da inventory outbox'ına yazar (raiseEvent true iken).
type StockLevelRepository interface {
	// GetByItem, kalemin stok kaydını döner; yoksa nil.
	GetByItem(ctx context.Context, tenantID, productItemID uuid.UUID) (*StockLevel, error)

	// Add, yeni stok kaydını ekler; raiseEvent true ise değişim olayı yazılır.
	Add(ctx context.Context, tenantID uuid.UUID, stock *StockLevel, raiseEvent bool) error

	// Update, stok kaydını kalıcılaştırır; raiseEvent true ise değişim olayı yazılır.
	Update(ctx context.Context, tenantID uuid.UUID, stock *StockLevel, raiseEvent bool) error
}

// CatalogItemGateway, kalemin Catalog'da var olduğunu doğrulayan ACL portudur
// (.NET ICatalogProductItemGateway karşılığı).
type CatalogItemGateway interface {
	// Exists, kalemin bu tenant'ta var olup olmadığını döner.
	Exists(ctx context.Context, tenantID, productItemID uuid.UUID) (bool, error)
}

// StockHandlers, stok kullanım senaryolarını yürütür.
type StockHandlers struct {
	stocks StockLevelRepository
	items  CatalogItemGateway
}

// NewStockHandlers, bağımlılıklarıyla stok handler'larını oluşturur.
func NewStockHandlers(stocks StockLevelRepository, items CatalogItemGateway) *StockHandlers {
	return &StockHandlers{stocks: stocks, items: items}
}

// Get, kalemin stok seviyesini döner; kayıt yoksa not_found (frontend bunu
// "kayıt yok" olarak yorumlar).
func (h *StockHandlers) Get(ctx context.Context, tenantID, productItemID uuid.UUID) sharedkernel.ResultOf[StockLevelDto] {
	stock, err := h.stocks.GetByItem(ctx, tenantID, productItemID)
	if err != nil {
		return sharedkernel.FailOf[StockLevelDto](sharedkernel.NewInternalError(err.Error()))
	}
	if stock == nil {
		return sharedkernel.FailOf[StockLevelDto](sharedkernel.NewNotFoundError("Stock level not found."))
	}
	return sharedkernel.OkOf(stockToDto(stock))
}

// Set, kalemin stok miktarını oluşturur/günceller (.NET SetStockHandler portu).
// Miktar değişmediyse olay yayımlanmaz — gereksiz kanal senkronu tetiklenmesin.
func (h *StockHandlers) Set(ctx context.Context, tenantID, productItemID uuid.UUID, quantity int) sharedkernel.ResultOf[StockLevelDto] {
	var f validationErrors
	if productItemID == uuid.Nil {
		f.add("product_item_id", sharedkernel.ValidationCodeInvalidID, "Id must be a valid identifier.")
	}
	if quantity < 0 {
		f.add("quantity", "GreaterThanOrEqualValidator", "Quantity cannot be negative.")
	}
	if verr := f.failure(); verr != nil {
		return sharedkernel.FailOf[StockLevelDto](verr)
	}

	exists, err := h.items.Exists(ctx, tenantID, productItemID)
	if err != nil {
		return sharedkernel.FailOf[StockLevelDto](sharedkernel.NewInternalError(err.Error()))
	}
	if !exists {
		return sharedkernel.FailOf[StockLevelDto](sharedkernel.NewNotFoundError("Product item not found."))
	}

	existing, err := h.stocks.GetByItem(ctx, tenantID, productItemID)
	if err != nil {
		return sharedkernel.FailOf[StockLevelDto](sharedkernel.NewInternalError(err.Error()))
	}
	if existing != nil {
		changed := existing.Quantity != quantity
		if changed {
			existing.Quantity = quantity
			existing.UpdatedAt = time.Now().UTC()
		}
		if err := h.stocks.Update(ctx, tenantID, existing, changed); err != nil {
			return sharedkernel.FailOf[StockLevelDto](sharedkernel.NewInternalError(err.Error()))
		}
		return sharedkernel.OkOf(stockToDto(existing))
	}

	stock := &StockLevel{ID: uuid.New(), ProductItemID: productItemID, Quantity: quantity, UpdatedAt: time.Now().UTC()}
	if err := h.stocks.Add(ctx, tenantID, stock, true); err != nil {
		return sharedkernel.FailOf[StockLevelDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(stockToDto(stock))
}

// validationErrors, modül-yerel doğrulama hata biriktiricisidir.
type validationErrors struct {
	errs []sharedkernel.ValidationError
}

// add, alan hatası ekler.
func (v *validationErrors) add(field, code, message string) {
	v.errs = append(v.errs, sharedkernel.ValidationError{Field: field, Code: code, Message: message})
}

// failure, biriken hataları doğrulama hatasına çevirir; yoksa nil.
func (v *validationErrors) failure() *sharedkernel.Error {
	if len(v.errs) == 0 {
		return nil
	}
	return sharedkernel.NewValidationError("One or more validation errors occurred.", v.errs...)
}
