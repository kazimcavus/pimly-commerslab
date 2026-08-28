package application

import (
	"context"
	"fmt"
	"strconv"
	"time"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// BarcodeSequence, sayısal barkod serisinin sıradaki değerini ve tahsis modunu
// tutan tenant başına tek satırlık ayardır (.NET Catalog.Domain.Barcodes.BarcodeSequence).
type BarcodeSequence struct {
	// NextValue, bir sonraki verilecek sayısal barkod değeridir.
	NextValue int64

	// ClientAllocationRequired, istemcinin ürün oluşturmadan önce allocate
	// ucunu çağırmasının zorunlu olup olmadığını belirtir.
	ClientAllocationRequired bool
}

// BarcodeSequenceSingletonID, tek satırın sabit kimliğidir.
const BarcodeSequenceSingletonID = 1

// BarcodeAllocation, üretilen tek bir barkod kaydıdır.
type BarcodeAllocation struct {
	ID          uuid.UUID
	Barcode     string
	AllocatedAt time.Time
}

// BarcodeSequenceDto, seri ayarının kablo biçimidir; next_preview sıradaki
// değerin dizgi hâlidir.
type BarcodeSequenceDto struct {
	NextValue                int64  `json:"next_value"`
	ClientAllocationRequired bool   `json:"client_allocation_required"`
	NextPreview              string `json:"next_preview"`
}

// AllocateBarcodesResultDto, tahsis yanıtının kablo biçimidir.
type AllocateBarcodesResultDto struct {
	Barcodes []string `json:"barcodes"`
}

// BarcodeAllocationDto, tahsis kaydının kablo biçimidir.
type BarcodeAllocationDto struct {
	ID          uuid.UUID `json:"id"`
	Barcode     string    `json:"barcode"`
	AllocatedAt time.Time `json:"allocated_at"`
}

// sequenceToDto, seriyi DTO'ya çevirir.
func sequenceToDto(s *BarcodeSequence) BarcodeSequenceDto {
	return BarcodeSequenceDto{
		NextValue:                s.NextValue,
		ClientAllocationRequired: s.ClientAllocationRequired,
		NextPreview:              strconv.FormatInt(s.NextValue, 10),
	}
}

// BarcodeRepository, barkod serisi ve tahsis kayıtlarının kalıcılık portudur.
type BarcodeRepository interface {
	// GetSequence, tenant'ın serisini döner; yoksa nil.
	GetSequence(ctx context.Context, tenantID uuid.UUID) (*BarcodeSequence, error)

	// AddSequence, başlangıç serisini ekler.
	AddSequence(ctx context.Context, tenantID uuid.UUID, sequence *BarcodeSequence) error

	// UpdateSequence, seriyi kalıcılaştırır.
	UpdateSequence(ctx context.Context, tenantID uuid.UUID, sequence *BarcodeSequence) error

	// MaxNumericBarcode, tahsis edilmiş en yüksek sayısal barkodu döner; yoksa 0.
	MaxNumericBarcode(ctx context.Context, tenantID uuid.UUID) (int64, error)

	// Allocate, seriden count kadar değeri ATOMİK ayırır (UPDATE..RETURNING),
	// tahsis kayıtlarını yazar ve barkodları döner; seri yoksa not_found.
	Allocate(ctx context.Context, tenantID uuid.UUID, count int) ([]BarcodeAllocation, *sharedkernel.Error)

	// ListAllocations, tahsisleri en yeniden eskiye sayfalanmış listeler.
	ListAllocations(ctx context.Context, tenantID uuid.UUID, p sharedkernel.Pagination) (sharedkernel.PagedResult[BarcodeAllocation], error)
}

// BarcodeHandlers, barkod kullanım senaryolarını yürütür (.NET'teki dört
// handler'ın Go karşılığı).
type BarcodeHandlers struct {
	barcodes BarcodeRepository
}

// NewBarcodeHandlers, bağımlılıklarıyla barkod handler'larını oluşturur.
func NewBarcodeHandlers(barcodes BarcodeRepository) *BarcodeHandlers {
	return &BarcodeHandlers{barcodes: barcodes}
}

// ensureSequence, seriyi döner; yoksa başlangıç serisini oluşturur
// (.NET handler'larındaki get-or-create deseni).
func (h *BarcodeHandlers) ensureSequence(ctx context.Context, tenantID uuid.UUID) (*BarcodeSequence, error) {
	sequence, err := h.barcodes.GetSequence(ctx, tenantID)
	if err != nil {
		return nil, err
	}
	if sequence == nil {
		sequence = &BarcodeSequence{NextValue: 1, ClientAllocationRequired: false}
		if err := h.barcodes.AddSequence(ctx, tenantID, sequence); err != nil {
			return nil, err
		}
	}
	return sequence, nil
}

// GetSequence, seri ayarını döner; yoksa varsayılanı oluşturup döner.
func (h *BarcodeHandlers) GetSequence(ctx context.Context, tenantID uuid.UUID) sharedkernel.ResultOf[BarcodeSequenceDto] {
	sequence, err := h.ensureSequence(ctx, tenantID)
	if err != nil {
		return sharedkernel.FailOf[BarcodeSequenceDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(sequenceToDto(sequence))
}

// UpdateSequence, sıradaki değer ile istemci tahsis modunu günceller; yeni
// değer tahsis edilmiş en yüksek barkodu aşmalıdır.
func (h *BarcodeHandlers) UpdateSequence(ctx context.Context, tenantID uuid.UUID, nextValue int64, clientAllocationRequired bool) sharedkernel.ResultOf[BarcodeSequenceDto] {
	if nextValue <= 0 {
		return sharedkernel.FailOf[BarcodeSequenceDto](sharedkernel.NewValidationError(
			"One or more validation errors occurred.",
			sharedkernel.ValidationError{Field: "next_value", Code: "GreaterThanValidator",
				Message: "Next value must be at least 1."}))
	}

	sequence, err := h.ensureSequence(ctx, tenantID)
	if err != nil {
		return sharedkernel.FailOf[BarcodeSequenceDto](sharedkernel.NewInternalError(err.Error()))
	}
	maxAllocated, err := h.barcodes.MaxNumericBarcode(ctx, tenantID)
	if err != nil {
		return sharedkernel.FailOf[BarcodeSequenceDto](sharedkernel.NewInternalError(err.Error()))
	}
	if nextValue <= maxAllocated {
		return sharedkernel.FailOf[BarcodeSequenceDto](sharedkernel.NewConflictError(fmt.Sprintf(
			"Next value must be greater than the highest allocated barcode (%d).", maxAllocated)))
	}

	sequence.NextValue = nextValue
	sequence.ClientAllocationRequired = clientAllocationRequired
	if err := h.barcodes.UpdateSequence(ctx, tenantID, sequence); err != nil {
		return sharedkernel.FailOf[BarcodeSequenceDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(sequenceToDto(sequence))
}

// Allocate, count kadar ardışık barkodu atomik ayırıp kayıtlarını yazar.
func (h *BarcodeHandlers) Allocate(ctx context.Context, tenantID uuid.UUID, count int) sharedkernel.ResultOf[AllocateBarcodesResultDto] {
	var f fieldErrors
	if count <= 0 {
		f.errs = append(f.errs, sharedkernel.ValidationError{
			Field: "count", Code: "GreaterThanValidator", Message: "Count must be at least 1."})
	}
	if count > 100 {
		f.errs = append(f.errs, sharedkernel.ValidationError{
			Field: "count", Code: "LessThanOrEqualValidator", Message: "Count cannot exceed 100."})
	}
	if verr := f.failure(); verr != nil {
		return sharedkernel.FailOf[AllocateBarcodesResultDto](verr)
	}

	if _, err := h.ensureSequence(ctx, tenantID); err != nil {
		return sharedkernel.FailOf[AllocateBarcodesResultDto](sharedkernel.NewInternalError(err.Error()))
	}
	allocations, aerr := h.barcodes.Allocate(ctx, tenantID, count)
	if aerr != nil {
		return sharedkernel.FailOf[AllocateBarcodesResultDto](aerr)
	}
	barcodes := make([]string, len(allocations))
	for i, allocation := range allocations {
		barcodes[i] = allocation.Barcode
	}
	return sharedkernel.OkOf(AllocateBarcodesResultDto{Barcodes: barcodes})
}

// ListAllocations, tahsis kayıtlarını sayfalanmış döner.
func (h *BarcodeHandlers) ListAllocations(ctx context.Context, tenantID uuid.UUID, page, pageSize int) sharedkernel.ResultOf[sharedkernel.PagedResult[BarcodeAllocationDto]] {
	pr := sharedkernel.ResolvePagination(page, pageSize)
	if pr.IsFailure() {
		return sharedkernel.FailOf[sharedkernel.PagedResult[BarcodeAllocationDto]](pr.Err())
	}
	pageResult, err := h.barcodes.ListAllocations(ctx, tenantID, pr.Value())
	if err != nil {
		return sharedkernel.FailOf[sharedkernel.PagedResult[BarcodeAllocationDto]](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(sharedkernel.MapPagedResult(pageResult, func(a BarcodeAllocation) BarcodeAllocationDto {
		return BarcodeAllocationDto{ID: a.ID, Barcode: a.Barcode, AllocatedAt: a.AllocatedAt}
	}))
}
