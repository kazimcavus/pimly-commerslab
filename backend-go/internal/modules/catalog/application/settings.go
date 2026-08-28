package application

import (
	"context"
	"strings"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// Slicer değeri ad konumları (.NET CatalogSettings sabitleri).
const (
	// SlicerNameSuffix: değer ürün adının sonuna eklenir ("Abiye Elbise - Beyaz").
	SlicerNameSuffix = "suffix"

	// SlicerNamePrefix: değer ürün adının başına eklenir ("Beyaz Abiye Elbise").
	SlicerNamePrefix = "prefix"
)

// CatalogSettings, katalog davranış tercihleridir — tenant başına tek satır.
type CatalogSettings struct {
	// SlicerNamePosition, ayraçlı (renk vb.) ürünlerde değer adının konumudur.
	SlicerNamePosition string
}

// CatalogSettingsDto, ayarların kablo biçimidir.
type CatalogSettingsDto struct {
	SlicerNamePosition string `json:"slicer_name_position"`
}

// CatalogSettingsRepository, ayarların kalıcılık portudur.
type CatalogSettingsRepository interface {
	// Get, tenant'ın ayarlarını döner; yoksa nil.
	Get(ctx context.Context, tenantID uuid.UUID) (*CatalogSettings, error)

	// Add, başlangıç ayarlarını ekler.
	Add(ctx context.Context, tenantID uuid.UUID, settings *CatalogSettings) error

	// Update, ayarları kalıcılaştırır.
	Update(ctx context.Context, tenantID uuid.UUID, settings *CatalogSettings) error
}

// CatalogSettingsHandlers, ayar uçlarını yürütür (.NET Get/UpdateCatalogSettings
// handler'larının Go karşılığı).
type CatalogSettingsHandlers struct {
	settings CatalogSettingsRepository
}

// NewCatalogSettingsHandlers, bağımlılıklarıyla handler'ları oluşturur.
func NewCatalogSettingsHandlers(settings CatalogSettingsRepository) *CatalogSettingsHandlers {
	return &CatalogSettingsHandlers{settings: settings}
}

// Get, ayarları döner; yoksa varsayılanları oluşturup döner.
func (h *CatalogSettingsHandlers) Get(ctx context.Context, tenantID uuid.UUID) sharedkernel.ResultOf[CatalogSettingsDto] {
	settings, err := h.settings.Get(ctx, tenantID)
	if err != nil {
		return sharedkernel.FailOf[CatalogSettingsDto](sharedkernel.NewInternalError(err.Error()))
	}
	if settings == nil {
		settings = &CatalogSettings{SlicerNamePosition: SlicerNameSuffix}
		if err := h.settings.Add(ctx, tenantID, settings); err != nil {
			return sharedkernel.FailOf[CatalogSettingsDto](sharedkernel.NewInternalError(err.Error()))
		}
	}
	return sharedkernel.OkOf(CatalogSettingsDto{SlicerNamePosition: settings.SlicerNamePosition})
}

// Update, tercihleri günceller; ayarlar yoksa önce oluşturur. Konum yalnızca
// "suffix" veya "prefix" olabilir.
func (h *CatalogSettingsHandlers) Update(ctx context.Context, tenantID uuid.UUID, slicerNamePosition string) sharedkernel.ResultOf[CatalogSettingsDto] {
	settings, err := h.settings.Get(ctx, tenantID)
	if err != nil {
		return sharedkernel.FailOf[CatalogSettingsDto](sharedkernel.NewInternalError(err.Error()))
	}
	isNew := settings == nil
	if isNew {
		settings = &CatalogSettings{SlicerNamePosition: SlicerNameSuffix}
	}

	normalized := strings.ToLower(strings.TrimSpace(slicerNamePosition))
	if normalized != SlicerNameSuffix && normalized != SlicerNamePrefix {
		return sharedkernel.FailOf[CatalogSettingsDto](sharedkernel.NewValidationError(
			"Slicer name position must be 'suffix' or 'prefix'."))
	}
	settings.SlicerNamePosition = normalized

	if isNew {
		if err := h.settings.Add(ctx, tenantID, settings); err != nil {
			return sharedkernel.FailOf[CatalogSettingsDto](sharedkernel.NewInternalError(err.Error()))
		}
	} else if err := h.settings.Update(ctx, tenantID, settings); err != nil {
		return sharedkernel.FailOf[CatalogSettingsDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(CatalogSettingsDto{SlicerNamePosition: settings.SlicerNamePosition})
}
