package sharedkernel

import "testing"

// Testler mevcut .NET test geleneğini izler: adlar İngilizce
// Subject_Condition_Outcome biçimindedir, açıklayıcı yorumlar Türkçedir.

func TestNewPagination_PageBelowOne_Fails(t *testing.T) {
	result := NewPagination(0, 20)
	if !result.IsFailure() {
		t.Fatal("0. sayfa doğrulama hatası üretmeliydi")
	}
	if result.Err().Code != ErrorCodeValidation {
		t.Fatalf("hata kodu %q bekleniyordu, %q geldi", ErrorCodeValidation, result.Err().Code)
	}
}

func TestNewPagination_PageSizeAboveMax_Fails(t *testing.T) {
	result := NewPagination(1, PaginationMaxPageSize+1)
	if !result.IsFailure() {
		t.Fatal("azami boyutu aşan sayfa boyutu doğrulama hatası üretmeliydi")
	}
}

func TestResolvePagination_ZeroValues_FallBackToDefaults(t *testing.T) {
	// İstemci page=0 gönderebilir (.NET endpoint parametre varsayılanı); normalize edilir.
	result := ResolvePagination(0, 0)
	if result.IsFailure() {
		t.Fatalf("beklenmeyen hata: %v", result.Err())
	}
	p := result.Value()
	if p.Page != PaginationDefaultPage || p.PageSize != PaginationDefaultPageSize {
		t.Fatalf("varsayılanlar bekleniyordu, %+v geldi", p)
	}
}

func TestNewPagedResult_TotalPages_RoundsUp(t *testing.T) {
	paged := NewPagedResult([]int{1, 2, 3}, Pagination{Page: 1, PageSize: 20}, 41)
	if paged.TotalPages != 3 {
		t.Fatalf("41 kayıt / 20 boyut = 3 sayfa bekleniyordu, %d geldi", paged.TotalPages)
	}
}

func TestNewPagedResult_NilItems_SerializesAsEmptySlice(t *testing.T) {
	// Frontend items alanının her zaman dizi olmasını bekler; nil dilim [] olmalıdır.
	paged := NewPagedResult[int](nil, Pagination{Page: 1, PageSize: 20}, 0)
	if paged.Items == nil {
		t.Fatal("Items nil kalmamalıydı")
	}
}

func TestPaginationSkip_SecondPage_ReturnsOffset(t *testing.T) {
	p := Pagination{Page: 3, PageSize: 20}
	if p.Skip() != 40 {
		t.Fatalf("OFFSET 40 bekleniyordu, %d geldi", p.Skip())
	}
}
