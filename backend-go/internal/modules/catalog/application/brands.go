package application

import (
	"context"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/catalog/domain/brands"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// BrandDto, marka veri transfer nesnesidir; JSON alanları kablo formatının
// parçasıdır (code null olabilir).
type BrandDto struct {
	ID   uuid.UUID `json:"id"`
	Name string    `json:"name"`
	Code *string   `json:"code"`
}

// brandToDto, domain markasını DTO'ya çevirir.
func brandToDto(b *brands.Brand) BrandDto {
	return BrandDto{ID: b.ID, Name: b.Name, Code: b.Code}
}

// BrandRepository, marka kalıcılık portudur (.NET IBrandRepository karşılığı).
// Tüm metodlar tenant kimliğini açıkça alır — Go tarafında görünmez tenant
// filtresi yoktur (bkz. sharedkernel/tenancy paket yorumu).
type BrandRepository interface {
	// GetByID, kimlikle markayı döner; yoksa nil.
	GetByID(ctx context.Context, tenantID, id uuid.UUID) (*brands.Brand, error)

	// GetByName, markayı ada göre (tenant içinde, büyük/küçük harf duyarsız)
	// döner; import'ta idempotent garanti için kullanılır. Yoksa nil.
	GetByName(ctx context.Context, tenantID uuid.UUID, name string) (*brands.Brand, error)

	// List, markaları ada göre sıralı ve sayfalanmış listeler.
	List(ctx context.Context, tenantID uuid.UUID, p sharedkernel.Pagination) (sharedkernel.PagedResult[*brands.Brand], error)

	// Add, yeni markayı kalıcı depoya ekler.
	Add(ctx context.Context, tenantID uuid.UUID, brand *brands.Brand) error

	// Update, marka değişikliklerini kalıcılaştırır.
	Update(ctx context.Context, tenantID uuid.UUID, brand *brands.Brand) error

	// Remove, markayı kalıcı depodan siler.
	Remove(ctx context.Context, tenantID, id uuid.UUID) error
}

// CreateBrandCommand, yeni marka isteğini taşır.
type CreateBrandCommand struct {
	Name string
	Code *string
}

// UpdateBrandCommand, marka güncelleme isteğini taşır.
type UpdateBrandCommand struct {
	ID   uuid.UUID
	Name string
	Code *string
}

// validateBrandInput, marka ad/kod kurallarını uygular
// (.NET Create/UpdateBrandCommandValidator portu: BrandName + BrandCode).
func validateBrandInput(name string, code *string) *sharedkernel.Error {
	var f fieldErrors
	f.required("name", "Name", name)
	f.maxLength("name", "Name", name, BrandNameMaxLength)
	f.maxLength("code", "Code", deref(code), BrandCodeMaxLength)
	return f.failure()
}

// BrandHandlers, marka kullanım senaryolarını yürütür (.NET'teki beş ayrı
// handler sınıfının Go karşılığı tek yapıda toplanmıştır; her metod bir
// handler'dır ve aynı sırayla aynı iş kurallarını uygular).
type BrandHandlers struct {
	brands BrandRepository
}

// NewBrandHandlers, bağımlılıklarıyla marka handler'larını oluşturur.
func NewBrandHandlers(brands BrandRepository) *BrandHandlers {
	return &BrandHandlers{brands: brands}
}

// Create, yeni marka oluşturur; aynı ada sahip marka varsa çakışma döner.
func (h *BrandHandlers) Create(ctx context.Context, tenantID uuid.UUID, cmd CreateBrandCommand) sharedkernel.ResultOf[BrandDto] {
	if verr := validateBrandInput(cmd.Name, cmd.Code); verr != nil {
		return sharedkernel.FailOf[BrandDto](verr)
	}

	existing, err := h.brands.GetByName(ctx, tenantID, cmd.Name)
	if err != nil {
		return sharedkernel.FailOf[BrandDto](sharedkernel.NewInternalError(err.Error()))
	}
	if existing != nil {
		return sharedkernel.FailOf[BrandDto](sharedkernel.NewConflictError("Brand with the same name already exists."))
	}

	createResult := brands.NewBrand(cmd.Name, cmd.Code)
	if createResult.IsFailure() {
		return sharedkernel.FailOf[BrandDto](createResult.Err())
	}
	brand := createResult.Value()

	if err := h.brands.Add(ctx, tenantID, brand); err != nil {
		return sharedkernel.FailOf[BrandDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(brandToDto(brand))
}

// List, markaları ada göre sıralı sayfalar halinde döner.
func (h *BrandHandlers) List(ctx context.Context, tenantID uuid.UUID, page, pageSize int) sharedkernel.ResultOf[sharedkernel.PagedResult[BrandDto]] {
	pr := sharedkernel.ResolvePagination(page, pageSize)
	if pr.IsFailure() {
		return sharedkernel.FailOf[sharedkernel.PagedResult[BrandDto]](pr.Err())
	}
	pageResult, err := h.brands.List(ctx, tenantID, pr.Value())
	if err != nil {
		return sharedkernel.FailOf[sharedkernel.PagedResult[BrandDto]](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(sharedkernel.MapPagedResult(pageResult, brandToDto))
}

// Get, tek markayı döner; yoksa not_found.
func (h *BrandHandlers) Get(ctx context.Context, tenantID, id uuid.UUID) sharedkernel.ResultOf[BrandDto] {
	brand, err := h.brands.GetByID(ctx, tenantID, id)
	if err != nil {
		return sharedkernel.FailOf[BrandDto](sharedkernel.NewInternalError(err.Error()))
	}
	if brand == nil {
		return sharedkernel.FailOf[BrandDto](sharedkernel.NewNotFoundError("Brand not found."))
	}
	return sharedkernel.OkOf(brandToDto(brand))
}

// Update, markayı yeniden adlandırır; ad başka bir markada kullanılıyorsa
// çakışma döner.
func (h *BrandHandlers) Update(ctx context.Context, tenantID uuid.UUID, cmd UpdateBrandCommand) sharedkernel.ResultOf[BrandDto] {
	if verr := validateBrandInput(cmd.Name, cmd.Code); verr != nil {
		return sharedkernel.FailOf[BrandDto](verr)
	}

	brand, err := h.brands.GetByID(ctx, tenantID, cmd.ID)
	if err != nil {
		return sharedkernel.FailOf[BrandDto](sharedkernel.NewInternalError(err.Error()))
	}
	if brand == nil {
		return sharedkernel.FailOf[BrandDto](sharedkernel.NewNotFoundError("Brand not found."))
	}

	duplicate, err := h.brands.GetByName(ctx, tenantID, cmd.Name)
	if err != nil {
		return sharedkernel.FailOf[BrandDto](sharedkernel.NewInternalError(err.Error()))
	}
	if duplicate != nil && duplicate.ID != brand.ID {
		return sharedkernel.FailOf[BrandDto](sharedkernel.NewConflictError("Brand with the same name already exists."))
	}

	if renameResult := brand.Rename(cmd.Name, cmd.Code); renameResult.IsFailure() {
		return sharedkernel.FailOf[BrandDto](renameResult.Err())
	}
	if err := h.brands.Update(ctx, tenantID, brand); err != nil {
		return sharedkernel.FailOf[BrandDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(brandToDto(brand))
}

// Delete, markayı siler; yoksa not_found.
func (h *BrandHandlers) Delete(ctx context.Context, tenantID, id uuid.UUID) sharedkernel.Result {
	brand, err := h.brands.GetByID(ctx, tenantID, id)
	if err != nil {
		return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
	}
	if brand == nil {
		return sharedkernel.Fail(sharedkernel.NewNotFoundError("Brand not found."))
	}
	if err := h.brands.Remove(ctx, tenantID, id); err != nil {
		return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.Ok()
}
