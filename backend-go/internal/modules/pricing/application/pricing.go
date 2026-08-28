package application

import (
	"context"
	"strings"
	"time"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// Alan uzunluk sınırları (.NET PricingValidationRules sabitleri).
const (
	PriceDefinitionNameMaxLength = 200
	PriceDefinitionCodeMaxLength = 100
)

// normalizeCurrency, para birimini normalize eder; boş değer TRY'dir.
func normalizeCurrency(currency *string) string {
	if currency == nil || strings.TrimSpace(*currency) == "" {
		return "TRY"
	}
	return strings.ToUpper(strings.TrimSpace(*currency))
}

// --- DTO'lar ---

// PriceDefinitionDto, fiyat tanımının kablo biçimidir.
type PriceDefinitionDto struct {
	ID   uuid.UUID `json:"id"`
	Name string    `json:"name"`
	Code *string   `json:"code"`
}

// ItemPriceDto, kalemin bir fiyat tanımındaki tutarının kablo biçimidir;
// definition_name okuma kolaylığı için tanımdan join edilir.
type ItemPriceDto struct {
	ID                uuid.UUID `json:"id"`
	ProductItemID     uuid.UUID `json:"product_item_id"`
	PriceDefinitionID uuid.UUID `json:"price_definition_id"`
	DefinitionName    string    `json:"definition_name"`
	Amount            Decimal   `json:"amount"`
	Currency          string    `json:"currency"`
	UpdatedAt         time.Time `json:"updated_at"`
}

// BasePriceDto, kalemin temel (site) fiyatının kablo biçimidir.
type BasePriceDto struct {
	ProductItemID   uuid.UUID `json:"product_item_id"`
	Amount          Decimal   `json:"amount"`
	CompareAtAmount *Decimal  `json:"compare_at_amount"`
	Currency        string    `json:"currency"`
	UpdatedAt       time.Time `json:"updated_at"`
}

// ChannelPriceDto, kalemin bir pazaryerindeki fiyatının kablo biçimidir.
type ChannelPriceDto struct {
	ProductItemID   uuid.UUID `json:"product_item_id"`
	Marketplace     string    `json:"marketplace"`
	Amount          Decimal   `json:"amount"`
	CompareAtAmount *Decimal  `json:"compare_at_amount"`
	Currency        string    `json:"currency"`
	UpdatedAt       time.Time `json:"updated_at"`
}

// --- kalıcılık kayıtları (domain varlıklarının Go karşılıkları) ---

// PriceDefinition, kullanıcı tanımlı fiyat alanıdır (ör. "TY Satış").
type PriceDefinition struct {
	ID   uuid.UUID
	Name string
	Code *string
}

// ItemPrice, kalem × fiyat tanımı başına tek tutar kaydıdır.
type ItemPrice struct {
	ID                uuid.UUID
	ProductItemID     uuid.UUID
	PriceDefinitionID uuid.UUID
	Amount            Decimal
	Currency          string
	UpdatedAt         time.Time
}

// BasePrice, kalemin temel fiyatıdır.
type BasePrice struct {
	ID              uuid.UUID
	ProductItemID   uuid.UUID
	Amount          Decimal
	CompareAtAmount *Decimal
	Currency        string
	UpdatedAt       time.Time
}

// ChannelPrice, kalemin bir pazaryerindeki fiyatıdır.
type ChannelPrice struct {
	ID              uuid.UUID
	ProductItemID   uuid.UUID
	MarketplaceCode string
	Amount          Decimal
	CompareAtAmount *Decimal
	Currency        string
	UpdatedAt       time.Time
}

// --- portlar ---

// PricingRepository, Pricing şemasının kalıcılık portudur. Kanal fiyatı
// yazımları ChannelPriceChanged olayını aynı transaction'da outbox'a düşürür.
type PricingRepository interface {
	// Fiyat tanımları.
	GetDefinition(ctx context.Context, tenantID, id uuid.UUID) (*PriceDefinition, error)
	GetDefinitionByName(ctx context.Context, tenantID uuid.UUID, name string) (*PriceDefinition, error)
	ListDefinitions(ctx context.Context, tenantID uuid.UUID, p sharedkernel.Pagination) (sharedkernel.PagedResult[*PriceDefinition], error)
	AddDefinition(ctx context.Context, tenantID uuid.UUID, definition *PriceDefinition) error
	UpdateDefinition(ctx context.Context, tenantID uuid.UUID, definition *PriceDefinition) error
	RemoveDefinition(ctx context.Context, tenantID, id uuid.UUID) error

	// Kalem fiyatları (tanım başına).
	ListItemPrices(ctx context.Context, tenantID, productItemID uuid.UUID) ([]ItemPriceRow, error)
	GetItemPrice(ctx context.Context, tenantID, productItemID, definitionID uuid.UUID) (*ItemPrice, error)
	UpsertItemPrice(ctx context.Context, tenantID uuid.UUID, price *ItemPrice) error
	RemoveItemPrice(ctx context.Context, tenantID, productItemID, definitionID uuid.UUID) (bool, error)

	// Temel fiyat.
	GetBasePrice(ctx context.Context, tenantID, productItemID uuid.UUID) (*BasePrice, error)
	UpsertBasePrice(ctx context.Context, tenantID uuid.UUID, price *BasePrice) error

	// Kanal fiyatları.
	ListChannelPrices(ctx context.Context, tenantID, productItemID uuid.UUID) ([]*ChannelPrice, error)
	GetChannelPrice(ctx context.Context, tenantID, productItemID uuid.UUID, marketplaceCode string) (*ChannelPrice, error)
	UpsertChannelPrice(ctx context.Context, tenantID uuid.UUID, price *ChannelPrice, raiseEvent bool) error
}

// ItemPriceRow, kalem fiyatının tanım adıyla birlikte okunmuş hâlidir.
type ItemPriceRow struct {
	ItemPrice
	DefinitionName string
}

// CatalogItemGateway, kalemin Catalog'da var olduğunu doğrulayan ACL portudur.
type CatalogItemGateway interface {
	Exists(ctx context.Context, tenantID, productItemID uuid.UUID) (bool, error)
}

// --- doğrulama yardımcıları ---

type fieldErrors struct{ errs []sharedkernel.ValidationError }

func (f *fieldErrors) add(field, code, message string) {
	f.errs = append(f.errs, sharedkernel.ValidationError{Field: field, Code: code, Message: message})
}

func (f *fieldErrors) failure() *sharedkernel.Error {
	if len(f.errs) == 0 {
		return nil
	}
	return sharedkernel.NewValidationError("One or more validation errors occurred.", f.errs...)
}

// validateDefinitionInput, fiyat tanımı ad/kod kurallarını uygular.
func validateDefinitionInput(name string, code *string) *sharedkernel.Error {
	var f fieldErrors
	if name == "" {
		f.add("name", sharedkernel.ValidationCodeRequired, "Name is required.")
	}
	if len([]rune(name)) > PriceDefinitionNameMaxLength {
		f.add("name", sharedkernel.ValidationCodeMaxLength, "Name must not exceed 200 characters.")
	}
	if code != nil && len([]rune(*code)) > PriceDefinitionCodeMaxLength {
		f.add("code", sharedkernel.ValidationCodeMaxLength, "Code must not exceed 100 characters.")
	}
	return f.failure()
}

// validateAmounts, tutar kurallarını uygular (negatif olamaz).
func validateAmounts(f *fieldErrors, amount Decimal, compareAt *Decimal) {
	if amount.IsNegative() {
		f.add("amount", "GreaterThanOrEqualValidator", "Amount cannot be negative.")
	}
	if compareAt != nil && compareAt.IsNegative() {
		f.add("compare_at_amount", "GreaterThanOrEqualValidator", "Compare-at amount cannot be negative.")
	}
}

// PricingHandlers, Pricing kullanım senaryolarını yürütür (.NET'teki 13 ayrı
// handler'ın Go karşılığı).
type PricingHandlers struct {
	repo  PricingRepository
	items CatalogItemGateway
}

// NewPricingHandlers, bağımlılıklarıyla handler'ları oluşturur.
func NewPricingHandlers(repo PricingRepository, items CatalogItemGateway) *PricingHandlers {
	return &PricingHandlers{repo: repo, items: items}
}

// --- fiyat tanımları ---

// definitionToDto, tanımı DTO'ya çevirir.
func definitionToDto(d *PriceDefinition) PriceDefinitionDto {
	return PriceDefinitionDto{ID: d.ID, Name: d.Name, Code: d.Code}
}

// CreateDefinition, yeni fiyat tanımı oluşturur; aynı ad conflict döner.
func (h *PricingHandlers) CreateDefinition(ctx context.Context, tenantID uuid.UUID, name string, code *string) sharedkernel.ResultOf[PriceDefinitionDto] {
	if verr := validateDefinitionInput(name, code); verr != nil {
		return sharedkernel.FailOf[PriceDefinitionDto](verr)
	}
	existing, err := h.repo.GetDefinitionByName(ctx, tenantID, name)
	if err != nil {
		return sharedkernel.FailOf[PriceDefinitionDto](sharedkernel.NewInternalError(err.Error()))
	}
	if existing != nil {
		return sharedkernel.FailOf[PriceDefinitionDto](sharedkernel.NewConflictError("Price definition with the same name already exists."))
	}

	definition := &PriceDefinition{ID: uuid.New(), Name: strings.TrimSpace(name), Code: trimToNil(code)}
	if err := h.repo.AddDefinition(ctx, tenantID, definition); err != nil {
		return sharedkernel.FailOf[PriceDefinitionDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(definitionToDto(definition))
}

// ListDefinitions, tanımları ada göre sayfalanmış döner.
func (h *PricingHandlers) ListDefinitions(ctx context.Context, tenantID uuid.UUID, page, pageSize int) sharedkernel.ResultOf[sharedkernel.PagedResult[PriceDefinitionDto]] {
	pr := sharedkernel.ResolvePagination(page, pageSize)
	if pr.IsFailure() {
		return sharedkernel.FailOf[sharedkernel.PagedResult[PriceDefinitionDto]](pr.Err())
	}
	pageResult, err := h.repo.ListDefinitions(ctx, tenantID, pr.Value())
	if err != nil {
		return sharedkernel.FailOf[sharedkernel.PagedResult[PriceDefinitionDto]](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(sharedkernel.MapPagedResult(pageResult, definitionToDto))
}

// GetDefinition, tek tanımı döner; yoksa not_found.
func (h *PricingHandlers) GetDefinition(ctx context.Context, tenantID, id uuid.UUID) sharedkernel.ResultOf[PriceDefinitionDto] {
	definition, err := h.repo.GetDefinition(ctx, tenantID, id)
	if err != nil {
		return sharedkernel.FailOf[PriceDefinitionDto](sharedkernel.NewInternalError(err.Error()))
	}
	if definition == nil {
		return sharedkernel.FailOf[PriceDefinitionDto](sharedkernel.NewNotFoundError("Price definition not found."))
	}
	return sharedkernel.OkOf(definitionToDto(definition))
}

// UpdateDefinition, tanımın adını/kodunu günceller.
func (h *PricingHandlers) UpdateDefinition(ctx context.Context, tenantID, id uuid.UUID, name string, code *string) sharedkernel.ResultOf[PriceDefinitionDto] {
	if verr := validateDefinitionInput(name, code); verr != nil {
		return sharedkernel.FailOf[PriceDefinitionDto](verr)
	}
	definition, err := h.repo.GetDefinition(ctx, tenantID, id)
	if err != nil {
		return sharedkernel.FailOf[PriceDefinitionDto](sharedkernel.NewInternalError(err.Error()))
	}
	if definition == nil {
		return sharedkernel.FailOf[PriceDefinitionDto](sharedkernel.NewNotFoundError("Price definition not found."))
	}
	duplicate, err := h.repo.GetDefinitionByName(ctx, tenantID, name)
	if err != nil {
		return sharedkernel.FailOf[PriceDefinitionDto](sharedkernel.NewInternalError(err.Error()))
	}
	if duplicate != nil && duplicate.ID != id {
		return sharedkernel.FailOf[PriceDefinitionDto](sharedkernel.NewConflictError("Price definition with the same name already exists."))
	}

	definition.Name = strings.TrimSpace(name)
	definition.Code = trimToNil(code)
	if err := h.repo.UpdateDefinition(ctx, tenantID, definition); err != nil {
		return sharedkernel.FailOf[PriceDefinitionDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(definitionToDto(definition))
}

// DeleteDefinition, tanımı siler; yoksa not_found.
func (h *PricingHandlers) DeleteDefinition(ctx context.Context, tenantID, id uuid.UUID) sharedkernel.Result {
	definition, err := h.repo.GetDefinition(ctx, tenantID, id)
	if err != nil {
		return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
	}
	if definition == nil {
		return sharedkernel.Fail(sharedkernel.NewNotFoundError("Price definition not found."))
	}
	if err := h.repo.RemoveDefinition(ctx, tenantID, id); err != nil {
		return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.Ok()
}

// --- kalem fiyatları ---

// ListItemPrices, kalemin tanım bazlı tüm fiyatlarını döner; kalem yoksa not_found.
func (h *PricingHandlers) ListItemPrices(ctx context.Context, tenantID, productItemID uuid.UUID) sharedkernel.ResultOf[[]ItemPriceDto] {
	exists, err := h.items.Exists(ctx, tenantID, productItemID)
	if err != nil {
		return sharedkernel.FailOf[[]ItemPriceDto](sharedkernel.NewInternalError(err.Error()))
	}
	if !exists {
		return sharedkernel.FailOf[[]ItemPriceDto](sharedkernel.NewNotFoundError("Product item not found."))
	}
	rows, err := h.repo.ListItemPrices(ctx, tenantID, productItemID)
	if err != nil {
		return sharedkernel.FailOf[[]ItemPriceDto](sharedkernel.NewInternalError(err.Error()))
	}
	dtos := make([]ItemPriceDto, len(rows))
	for i, row := range rows {
		dtos[i] = ItemPriceDto{
			ID: row.ID, ProductItemID: row.ProductItemID, PriceDefinitionID: row.PriceDefinitionID,
			DefinitionName: row.DefinitionName, Amount: row.Amount, Currency: row.Currency, UpdatedAt: row.UpdatedAt,
		}
	}
	return sharedkernel.OkOf(dtos)
}

// UpsertItemPrice, kalemin bir tanımdaki tutarını oluşturur/günceller.
func (h *PricingHandlers) UpsertItemPrice(ctx context.Context, tenantID, productItemID, definitionID uuid.UUID, amount Decimal, currency *string) sharedkernel.ResultOf[ItemPriceDto] {
	var f fieldErrors
	if amount.IsNegative() {
		f.add("amount", "GreaterThanOrEqualValidator", "Amount cannot be negative.")
	}
	if verr := f.failure(); verr != nil {
		return sharedkernel.FailOf[ItemPriceDto](verr)
	}

	exists, err := h.items.Exists(ctx, tenantID, productItemID)
	if err != nil {
		return sharedkernel.FailOf[ItemPriceDto](sharedkernel.NewInternalError(err.Error()))
	}
	if !exists {
		return sharedkernel.FailOf[ItemPriceDto](sharedkernel.NewNotFoundError("Product item not found."))
	}
	definition, err := h.repo.GetDefinition(ctx, tenantID, definitionID)
	if err != nil {
		return sharedkernel.FailOf[ItemPriceDto](sharedkernel.NewInternalError(err.Error()))
	}
	if definition == nil {
		return sharedkernel.FailOf[ItemPriceDto](sharedkernel.NewNotFoundError("Price definition not found."))
	}

	price, err := h.repo.GetItemPrice(ctx, tenantID, productItemID, definitionID)
	if err != nil {
		return sharedkernel.FailOf[ItemPriceDto](sharedkernel.NewInternalError(err.Error()))
	}
	if price == nil {
		price = &ItemPrice{ID: uuid.New(), ProductItemID: productItemID, PriceDefinitionID: definitionID}
	}
	price.Amount = amount
	price.Currency = normalizeCurrency(currency)
	price.UpdatedAt = time.Now().UTC()
	if err := h.repo.UpsertItemPrice(ctx, tenantID, price); err != nil {
		return sharedkernel.FailOf[ItemPriceDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(ItemPriceDto{
		ID: price.ID, ProductItemID: price.ProductItemID, PriceDefinitionID: price.PriceDefinitionID,
		DefinitionName: definition.Name, Amount: price.Amount, Currency: price.Currency, UpdatedAt: price.UpdatedAt,
	})
}

// DeleteItemPrice, kalemin bir tanımdaki tutarını siler; kayıt yoksa not_found.
func (h *PricingHandlers) DeleteItemPrice(ctx context.Context, tenantID, productItemID, definitionID uuid.UUID) sharedkernel.Result {
	removed, err := h.repo.RemoveItemPrice(ctx, tenantID, productItemID, definitionID)
	if err != nil {
		return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
	}
	if !removed {
		return sharedkernel.Fail(sharedkernel.NewNotFoundError("Item price not found."))
	}
	return sharedkernel.Ok()
}

// --- temel fiyat ---

// GetBasePrice, kalemin temel fiyatını döner; kayıt yoksa not_found.
func (h *PricingHandlers) GetBasePrice(ctx context.Context, tenantID, productItemID uuid.UUID) sharedkernel.ResultOf[BasePriceDto] {
	price, err := h.repo.GetBasePrice(ctx, tenantID, productItemID)
	if err != nil {
		return sharedkernel.FailOf[BasePriceDto](sharedkernel.NewInternalError(err.Error()))
	}
	if price == nil {
		return sharedkernel.FailOf[BasePriceDto](sharedkernel.NewNotFoundError("Base price not found."))
	}
	return sharedkernel.OkOf(BasePriceDto{
		ProductItemID: price.ProductItemID, Amount: price.Amount,
		CompareAtAmount: price.CompareAtAmount, Currency: price.Currency, UpdatedAt: price.UpdatedAt,
	})
}

// SetBasePrice, kalemin temel fiyatını oluşturur/günceller.
func (h *PricingHandlers) SetBasePrice(ctx context.Context, tenantID, productItemID uuid.UUID, amount Decimal, compareAt *Decimal, currency *string) sharedkernel.ResultOf[BasePriceDto] {
	var f fieldErrors
	validateAmounts(&f, amount, compareAt)
	if verr := f.failure(); verr != nil {
		return sharedkernel.FailOf[BasePriceDto](verr)
	}

	exists, err := h.items.Exists(ctx, tenantID, productItemID)
	if err != nil {
		return sharedkernel.FailOf[BasePriceDto](sharedkernel.NewInternalError(err.Error()))
	}
	if !exists {
		return sharedkernel.FailOf[BasePriceDto](sharedkernel.NewNotFoundError("Product item not found."))
	}

	price, err := h.repo.GetBasePrice(ctx, tenantID, productItemID)
	if err != nil {
		return sharedkernel.FailOf[BasePriceDto](sharedkernel.NewInternalError(err.Error()))
	}
	if price == nil {
		price = &BasePrice{ID: uuid.New(), ProductItemID: productItemID}
	}
	price.Amount = amount
	price.CompareAtAmount = compareAt
	price.Currency = normalizeCurrency(currency)
	price.UpdatedAt = time.Now().UTC()
	if err := h.repo.UpsertBasePrice(ctx, tenantID, price); err != nil {
		return sharedkernel.FailOf[BasePriceDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(BasePriceDto{
		ProductItemID: price.ProductItemID, Amount: price.Amount,
		CompareAtAmount: price.CompareAtAmount, Currency: price.Currency, UpdatedAt: price.UpdatedAt,
	})
}

// --- kanal fiyatları ---

// channelPriceToDto, kanal fiyatını DTO'ya çevirir.
func channelPriceToDto(p *ChannelPrice) ChannelPriceDto {
	return ChannelPriceDto{
		ProductItemID: p.ProductItemID, Marketplace: p.MarketplaceCode, Amount: p.Amount,
		CompareAtAmount: p.CompareAtAmount, Currency: p.Currency, UpdatedAt: p.UpdatedAt,
	}
}

// ListChannelPrices, kalemin tüm pazaryeri fiyatlarını döner.
func (h *PricingHandlers) ListChannelPrices(ctx context.Context, tenantID, productItemID uuid.UUID) sharedkernel.ResultOf[[]ChannelPriceDto] {
	prices, err := h.repo.ListChannelPrices(ctx, tenantID, productItemID)
	if err != nil {
		return sharedkernel.FailOf[[]ChannelPriceDto](sharedkernel.NewInternalError(err.Error()))
	}
	dtos := make([]ChannelPriceDto, len(prices))
	for i, price := range prices {
		dtos[i] = channelPriceToDto(price)
	}
	return sharedkernel.OkOf(dtos)
}

// GetChannelPrice, kalemin bir pazaryerindeki fiyatını döner; kayıt yoksa not_found.
func (h *PricingHandlers) GetChannelPrice(ctx context.Context, tenantID, productItemID uuid.UUID, marketplaceCode string) sharedkernel.ResultOf[ChannelPriceDto] {
	marketplace := sharedkernel.MarketplaceFromCode(marketplaceCode)
	if marketplace.IsFailure() {
		return sharedkernel.FailOf[ChannelPriceDto](marketplace.Err())
	}
	price, err := h.repo.GetChannelPrice(ctx, tenantID, productItemID, marketplace.Value().Code())
	if err != nil {
		return sharedkernel.FailOf[ChannelPriceDto](sharedkernel.NewInternalError(err.Error()))
	}
	if price == nil {
		return sharedkernel.FailOf[ChannelPriceDto](sharedkernel.NewNotFoundError("Channel price not found."))
	}
	return sharedkernel.OkOf(channelPriceToDto(price))
}

// SetChannelPrice, kalemin bir pazaryerindeki fiyatını oluşturur/günceller.
// Değerler değişmediyse olay yayımlanmaz — gereksiz kanal senkronu tetiklenmesin.
func (h *PricingHandlers) SetChannelPrice(ctx context.Context, tenantID, productItemID uuid.UUID, marketplaceCode string, amount Decimal, compareAt *Decimal, currency *string) sharedkernel.ResultOf[ChannelPriceDto] {
	var f fieldErrors
	if strings.TrimSpace(marketplaceCode) == "" {
		f.add("marketplace", "NotEmptyValidator", "Marketplace is required.")
	}
	validateAmounts(&f, amount, compareAt)
	if verr := f.failure(); verr != nil {
		return sharedkernel.FailOf[ChannelPriceDto](verr)
	}

	marketplace := sharedkernel.MarketplaceFromCode(marketplaceCode)
	if marketplace.IsFailure() {
		return sharedkernel.FailOf[ChannelPriceDto](marketplace.Err())
	}
	exists, err := h.items.Exists(ctx, tenantID, productItemID)
	if err != nil {
		return sharedkernel.FailOf[ChannelPriceDto](sharedkernel.NewInternalError(err.Error()))
	}
	if !exists {
		return sharedkernel.FailOf[ChannelPriceDto](sharedkernel.NewNotFoundError("Product item not found."))
	}

	price, err := h.repo.GetChannelPrice(ctx, tenantID, productItemID, marketplace.Value().Code())
	if err != nil {
		return sharedkernel.FailOf[ChannelPriceDto](sharedkernel.NewInternalError(err.Error()))
	}

	normalizedCurrency := normalizeCurrency(currency)
	changed := true
	if price != nil {
		changed = !price.Amount.Equal(amount) ||
			!EqualPtr(price.CompareAtAmount, compareAt) ||
			price.Currency != normalizedCurrency
		if changed {
			price.Amount = amount
			price.CompareAtAmount = compareAt
			price.Currency = normalizedCurrency
			price.UpdatedAt = time.Now().UTC()
		}
	} else {
		price = &ChannelPrice{
			ID: uuid.New(), ProductItemID: productItemID, MarketplaceCode: marketplace.Value().Code(),
			Amount: amount, CompareAtAmount: compareAt, Currency: normalizedCurrency, UpdatedAt: time.Now().UTC(),
		}
	}
	if err := h.repo.UpsertChannelPrice(ctx, tenantID, price, changed); err != nil {
		return sharedkernel.FailOf[ChannelPriceDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(channelPriceToDto(price))
}

// trimToNil, opsiyonel dizgiyi kırpar; boş değer nil'e çevrilir.
func trimToNil(s *string) *string {
	if s == nil {
		return nil
	}
	trimmed := strings.TrimSpace(*s)
	if trimmed == "" {
		return nil
	}
	return &trimmed
}
