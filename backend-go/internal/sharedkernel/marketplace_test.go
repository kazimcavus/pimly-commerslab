package sharedkernel

import "testing"

func TestMarketplaceFromCode_KnownCode_Succeeds(t *testing.T) {
	result := MarketplaceFromCode("TY")
	if result.IsFailure() {
		t.Fatalf("beklenmeyen hata: %v", result.Err())
	}
	if result.Value().Name() != "Trendyol" {
		t.Fatalf("Trendyol bekleniyordu, %q geldi", result.Value().Name())
	}
}

func TestMarketplaceFromCode_UnknownCode_Fails(t *testing.T) {
	result := MarketplaceFromCode("XX")
	if !result.IsFailure() {
		t.Fatal("bilinmeyen kod doğrulama hatası üretmeliydi")
	}
	if result.Err().Code != ErrorCodeValidation {
		t.Fatalf("hata kodu %q bekleniyordu, %q geldi", ErrorCodeValidation, result.Err().Code)
	}
}

func TestMarketplaceFromPersistence_UnknownCode_Panics(t *testing.T) {
	// Veritabanındaki bilinmeyen kod veri bütünlüğü hatasıdır; sessizce geçilmez.
	defer func() {
		if recover() == nil {
			t.Fatal("bilinmeyen kalıcı kod panic üretmeliydi")
		}
	}()
	MarketplaceFromPersistence("XX")
}
