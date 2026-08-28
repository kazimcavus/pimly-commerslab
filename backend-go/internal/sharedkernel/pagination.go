package sharedkernel

import "fmt"

// Sayfalama varsayılanları. Değerler .NET SharedKernel.Pagination ile birebir
// aynıdır; frontend'in api.js dosyası MaxPageSize=100'e sabit bağımlıdır.
const (
	// PaginationDefaultPage, sayfa belirtilmediğinde kullanılan 1 tabanlı sayfa numarasıdır.
	PaginationDefaultPage = 1

	// PaginationDefaultPageSize, sayfa boyutu belirtilmediğinde kullanılan değerdir.
	PaginationDefaultPageSize = 20

	// PaginationMaxPageSize, tek istekte dönebilecek azami kayıt sayısıdır.
	PaginationMaxPageSize = 100
)

// Pagination, doğrulanmış sayfalama parametrelerini taşır.
type Pagination struct {
	// Page, 1 tabanlı sayfa numarasıdır.
	Page int

	// PageSize, sayfa başına kayıt sayısıdır (1..PaginationMaxPageSize).
	PageSize int
}

// Skip, SQL OFFSET değerini döner.
func (p Pagination) Skip() int { return (p.Page - 1) * p.PageSize }

// NewPagination, ham sayfa/boyut değerlerini doğrulayıp Pagination üretir.
// Hata mesajları .NET karşılığıyla birebir aynıdır (parite testleri doğrular).
func NewPagination(page, pageSize int) ResultOf[Pagination] {
	if page < PaginationDefaultPage {
		return FailOf[Pagination](NewValidationError("Page must be at least 1."))
	}
	if pageSize < 1 || pageSize > PaginationMaxPageSize {
		return FailOf[Pagination](NewValidationError(
			fmt.Sprintf("Page size must be between 1 and %d.", PaginationMaxPageSize)))
	}
	return OkOf(Pagination{Page: page, PageSize: pageSize})
}

// ResolvePagination, HTTP katmanından gelen ham değerleri normalize eder:
// 0 veya negatif değerler varsayılana düşer (.NET endpoint'lerindeki
// PaginationSupport.Resolve davranışının karşılığı — istemci page=0 da gönderebilir),
// aralık dışı değerler doğrulama hatası üretir.
func ResolvePagination(page, pageSize int) ResultOf[Pagination] {
	if page <= 0 {
		page = PaginationDefaultPage
	}
	if pageSize <= 0 {
		pageSize = PaginationDefaultPageSize
	}
	return NewPagination(page, pageSize)
}

// PagedResult, sayfalanmış liste sonucunu taşır. JSON alan adları
// (items, page, page_size, total_count, total_pages) frontend sözleşmesinin
// parçasıdır ve değiştirilemez.
type PagedResult[T any] struct {
	Items      []T `json:"items"`
	Page       int `json:"page"`
	PageSize   int `json:"page_size"`
	TotalCount int `json:"total_count"`
	TotalPages int `json:"total_pages"`
}

// NewPagedResult, sayfa içeriği ve toplam kayıt sayısından zarfı kurar;
// TotalPages değerini .NET karşılığıyla aynı formülle (yukarı yuvarlama) hesaplar.
// items nil ise boş dilime çevrilir ki JSON çıktısı null yerine [] olsun.
func NewPagedResult[T any](items []T, p Pagination, totalCount int) PagedResult[T] {
	if items == nil {
		items = []T{}
	}
	totalPages := 0
	if p.PageSize > 0 {
		totalPages = (totalCount + p.PageSize - 1) / p.PageSize
	}
	return PagedResult[T]{
		Items:      items,
		Page:       p.Page,
		PageSize:   p.PageSize,
		TotalCount: totalCount,
		TotalPages: totalPages,
	}
}

// MapPagedResult, sayfalı sonucu başka bir öğe türüne dönüştürür (ör. domain → DTO).
func MapPagedResult[T, U any](src PagedResult[T], fn func(T) U) PagedResult[U] {
	items := make([]U, len(src.Items))
	for i, it := range src.Items {
		items[i] = fn(it)
	}
	return PagedResult[U]{
		Items:      items,
		Page:       src.Page,
		PageSize:   src.PageSize,
		TotalCount: src.TotalCount,
		TotalPages: src.TotalPages,
	}
}
