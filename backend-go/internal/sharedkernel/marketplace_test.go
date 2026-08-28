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

func TestMarketplaceFromCode_UnknownCode_ReturnsNotFound(t *testing.T) {
	// .NET Marketplace.FromCode bilinmeyen kod için NotFound döner (parite
	// golden'ı pricing/channel_price_unknown_marketplace ile doğrulanmıştır).
	result := MarketplaceFromCode("XX")
	if !result.IsFailure() {
		t.Fatal("bilinmeyen kod hata üretmeliydi")
	}
	if result.Err().Code != ErrorCodeNotFound {
		t.Fatalf("hata kodu %q bekleniyordu, %q geldi", ErrorCodeNotFound, result.Err().Code)
	}
}

func TestMarketplaceFromCode_LowercaseCode_IsNormalized(t *testing.T) {
	result := MarketplaceFromCode(" ty ")
	if result.IsFailure() {
		t.Fatalf("küçük harfli kod normalize edilmeliydi: %v", result.Err())
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
