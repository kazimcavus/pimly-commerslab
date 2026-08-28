package application

import (
	"context"
	"strings"
	"time"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/catalog/domain/products"
	"pimly.commerslab/backend-go/internal/modules/catalog/domain/skugen"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// SkuSegmentDto, SKU segmentinin kablo biçimidir.
type SkuSegmentDto struct {
	Type   string  `json:"type"`
	Label  *string `json:"label"`
	Value  *string `json:"value"`
	Start  *int    `json:"start"`
	Width  *int    `json:"width"`
	Digits *int    `json:"digits"`
	Source *string `json:"source"`
}

// SkuGeneratorConfigDto, SKU oluşturucu yapılandırmasının kablo biçimidir.
type SkuGeneratorConfigDto struct {
	Enabled          bool            `json:"enabled"`
	Segments         []SkuSegmentDto `json:"segments"`
	CounterNextValue int64           `json:"counter_next_value"`
}

// skuConfigToDto, domain yapılandırmasını DTO'ya çevirir.
func skuConfigToDto(config *skugen.Config) SkuGeneratorConfigDto {
	segments := make([]SkuSegmentDto, len(config.Segments))
	for i, s := range config.Segments {
		segments[i] = SkuSegmentDto{Type: s.Type, Label: s.Label, Value: s.Value,
			Start: s.Start, Width: s.Width, Digits: s.Digits, Source: s.Source}
	}
	return SkuGeneratorConfigDto{Enabled: config.Enabled, Segments: segments, CounterNextValue: config.CounterNextValue}
}

// SkuConfigRepository, SKU yapılandırması kalıcılık portudur.
type SkuConfigRepository interface {
	// Get, tenant'ın yapılandırmasını döner; yoksa nil.
	Get(ctx context.Context, tenantID uuid.UUID) (*skugen.Config, error)

	// Add, yeni yapılandırma satırını ekler.
	Add(ctx context.Context, tenantID uuid.UUID, config *skugen.Config) error

	// Update, yapılandırmayı kalıcılaştırır.
	Update(ctx context.Context, tenantID uuid.UUID, config *skugen.Config) error
}

// SkuCounterAllocator, sayaç bloğu rezervasyon portudur; Reserve count kadar
// değeri atomik ayırır ve bloğun başlangıcını döner.
type SkuCounterAllocator interface {
	Reserve(ctx context.Context, tenantID uuid.UUID, count int) (int64, *sharedkernel.Error)
}

// UpdateSkuGeneratorConfigCommand, yapılandırma güncelleme komutudur.
type UpdateSkuGeneratorConfigCommand struct {
	Enabled          bool
	Segments         []SkuSegmentDto
	CounterNextValue *int64
}

// SkuGeneratorHandlers, SKU yapılandırma uçlarını ve plan üretimini yürütür
// (.NET SkuGeneratorService + config handler'larının Go karşılığı).
type SkuGeneratorHandlers struct {
	configs   SkuConfigRepository
	allocator SkuCounterAllocator
}

// NewSkuGeneratorHandlers, bağımlılıklarıyla handler'ları oluşturur.
func NewSkuGeneratorHandlers(configs SkuConfigRepository, allocator SkuCounterAllocator) *SkuGeneratorHandlers {
	return &SkuGeneratorHandlers{configs: configs, allocator: allocator}
}

// GetConfig, yapılandırmayı döner; yoksa kapalı başlangıç yapılandırması
// oluşturup kalıcılaştırır (.NET GetSkuGeneratorConfigHandler davranışı).
func (h *SkuGeneratorHandlers) GetConfig(ctx context.Context, tenantID uuid.UUID) sharedkernel.ResultOf[SkuGeneratorConfigDto] {
	config, err := h.configs.Get(ctx, tenantID)
	if err != nil {
		return sharedkernel.FailOf[SkuGeneratorConfigDto](sharedkernel.NewInternalError(err.Error()))
	}
	if config == nil {
		config = skugen.NewInitialConfig()
		if err := h.configs.Add(ctx, tenantID, config); err != nil {
			return sharedkernel.FailOf[SkuGeneratorConfigDto](sharedkernel.NewInternalError(err.Error()))
		}
	}
	return sharedkernel.OkOf(skuConfigToDto(config))
}

// UpdateConfig, yapılandırmayı günceller (.NET UpdateSkuGeneratorConfigHandler portu).
func (h *SkuGeneratorHandlers) UpdateConfig(ctx context.Context, tenantID uuid.UUID, cmd UpdateSkuGeneratorConfigCommand) sharedkernel.ResultOf[SkuGeneratorConfigDto] {
	var f fieldErrors
	if cmd.Segments == nil {
		f.errs = append(f.errs, sharedkernel.ValidationError{
			Field: "segments", Code: sharedkernel.ValidationCodeRequired, Message: "Segments is required."})
	}
	for i, segment := range cmd.Segments {
		if strings.TrimSpace(segment.Type) == "" {
			f.errs = append(f.errs, sharedkernel.ValidationError{
				Field:   "segments[" + itoa(i) + "].type",
				Code:    sharedkernel.ValidationCodeRequired,
				Message: "Segment type is required."})
		}
	}
	if cmd.CounterNextValue != nil && *cmd.CounterNextValue <= 0 {
		f.errs = append(f.errs, sharedkernel.ValidationError{
			Field: "counter_next_value", Code: sharedkernel.ValidationCodeUnknown,
			Message: "Counter next value must be at least 1."})
	}
	if verr := f.failure(); verr != nil {
		return sharedkernel.FailOf[SkuGeneratorConfigDto](verr)
	}

	config, err := h.configs.Get(ctx, tenantID)
	if err != nil {
		return sharedkernel.FailOf[SkuGeneratorConfigDto](sharedkernel.NewInternalError(err.Error()))
	}
	if config == nil {
		return sharedkernel.FailOf[SkuGeneratorConfigDto](sharedkernel.NewNotFoundError("SKU generator is not configured."))
	}

	segments := make([]skugen.Segment, len(cmd.Segments))
	for i, s := range cmd.Segments {
		segments[i] = skugen.Segment{Type: s.Type, Label: s.Label, Value: s.Value,
			Start: s.Start, Width: s.Width, Digits: s.Digits, Source: s.Source}
	}
	if updateResult := config.UpdateSettings(cmd.Enabled, segments, cmd.CounterNextValue); updateResult.IsFailure() {
		return sharedkernel.FailOf[SkuGeneratorConfigDto](updateResult.Err())
	}
	if err := h.configs.Update(ctx, tenantID, config); err != nil {
		return sharedkernel.FailOf[SkuGeneratorConfigDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(skuConfigToDto(config))
}

// BuildPlans, oluşturma planlarını üretir (.NET SkuGeneratorService.BuildPlansAsync
// portu): generator açık ve model kodu boşsa kodlar şablondan üretilir; değilse
// verilen model kodu bölme (split) girdisi olur.
func (h *SkuGeneratorHandlers) BuildPlans(ctx context.Context, tenantID uuid.UUID, modelCode string, codeInputs []string,
	name string, variantRefs []products.VariantRef, drafts []products.ItemDraft,
	splitOverrides []products.SplitOverride) sharedkernel.ResultOf[[]products.CreatePlan] {

	config, err := h.configs.Get(ctx, tenantID)
	if err != nil {
		return sharedkernel.FailOf[[]products.CreatePlan](sharedkernel.NewInternalError(err.Error()))
	}
	useGenerator := config != nil && config.Enabled && strings.TrimSpace(modelCode) == ""

	if useGenerator {
		if manual := skugen.ValidateManualInputs(config.Segments, codeInputs); manual.IsFailure() {
			return sharedkernel.FailOf[[]products.CreatePlan](manual.Err())
		}
		for _, draft := range drafts {
			if variant := skugen.ValidateVariantCodes(config.Segments, draftSelections(draft)); variant.IsFailure() {
				return sharedkernel.FailOf[[]products.CreatePlan](variant.Err())
			}
		}
	} else if strings.TrimSpace(modelCode) == "" {
		return sharedkernel.FailOf[[]products.CreatePlan](sharedkernel.NewValidationError("Model code is required."))
	}

	baseForSplit := strings.TrimSpace(modelCode)
	if useGenerator {
		baseForSplit = skugen.BasePlaceholder
	}
	splitResult := products.Split(baseForSplit, name, variantRefs, drafts, splitOverrides)
	if splitResult.IsFailure() {
		return sharedkernel.FailOf[[]products.CreatePlan](splitResult.Err())
	}
	plans := splitResult.Value()

	var counter int64 = 1
	if config != nil {
		counter = config.CounterNextValue
	}
	if useGenerator {
		config.EnsureCounterInitialized()
		counter = config.CounterNextValue
		totalUses := len(plans) * config.CounterSegmentCount()
		if totalUses > 0 {
			start, aerr := h.allocator.Reserve(ctx, tenantID, totalUses)
			if aerr != nil {
				return sharedkernel.FailOf[[]products.CreatePlan](aerr)
			}
			counter = start
		}
	}

	finalPlans := make([]products.CreatePlan, 0, len(plans))
	for _, plan := range plans {
		finalModelCode := plan.ModelCode
		if useGenerator {
			assembled := skugen.AssembleProductCode(config.Segments, codeInputs, counter, time.Now().UTC())
			if assembled.IsFailure() {
				return sharedkernel.FailOf[[]products.CreatePlan](assembled.Err())
			}
			counter = assembled.Value().NextCounter
			finalModelCode = strings.ReplaceAll(plan.ModelCode, skugen.BasePlaceholder, assembled.Value().Code)
		}

		items := make([]products.ItemDraft, len(plan.Items))
		for i, draft := range plan.Items {
			items[i] = applyVariantSku(config, useGenerator, finalModelCode, draft)
		}

		// Generator yolunda her plan sayaçtan kendi kodunu alır; paylaşılan
		// grup kodu anlamsızdır.
		groupCode := plan.GroupCode
		if useGenerator {
			groupCode = nil
		}
		plan.ModelCode = finalModelCode
		plan.Items = items
		plan.GroupCode = groupCode
		finalPlans = append(finalPlans, plan)
	}
	return sharedkernel.OkOf(finalPlans)
}

// draftSelections, taslağın eksen seçimlerini SKU üretim biçimine çevirir.
func draftSelections(draft products.ItemDraft) []skugen.VariantSelection {
	selections := make([]skugen.VariantSelection, len(draft.VariantValues))
	for i, value := range draft.VariantValues {
		selections[i] = skugen.VariantSelection{
			SelectionStyle: value.Variant.SelectionStyle, Name: value.Name, Key: value.Key}
	}
	return selections
}

// applyVariantSku, generator yolunda SKU'su boş kalemlere üretilmiş SKU atar.
func applyVariantSku(config *skugen.Config, useGenerator bool, modelCode string, draft products.ItemDraft) products.ItemDraft {
	if !useGenerator || config == nil || (draft.Sku != nil && strings.TrimSpace(*draft.Sku) != "") {
		return draft
	}
	sku := skugen.AssembleVariantSku(modelCode, config.Segments, draftSelections(draft))
	draft.Sku = &sku
	return draft
}

// itoa, küçük yardımcı: negatif olmayan tamsayıyı dizgiye çevirir.
func itoa(n int) string {
	if n == 0 {
		return "0"
	}
	digits := []byte{}
	for n > 0 {
		digits = append([]byte{byte('0' + n%10)}, digits...)
		n /= 10
	}
	return string(digits)
}
