package users

import "testing"

// dotnetGoldenHash, .NET tarafındaki ASP.NET Identity PasswordHasher'ın
// "demo1234" şifresi için ürettiği GERÇEK bir V3 özetidir (geliştirme seed
// kullanıcısından alınmıştır). Bu test, Go doğrulayıcısının .NET ile bayt
// uyumlu olduğunun kalıcı kanıtıdır; hash'i asla değiştirmeyin.
const dotnetGoldenHash = "AQAAAAIAAYagAAAAEPRg5c+amu/B/dAZrEA4ESYIZtDnLPJNjRPGF/kgXoLRcF5za52SROHziNC92jaCEg=="

func TestVerifyPassword_DotnetGeneratedHash_CorrectPassword_Succeeds(t *testing.T) {
	if !VerifyPassword("demo1234", dotnetGoldenHash) {
		t.Fatal(".NET tarafından üretilmiş V3 hash doğru şifreyle doğrulanamadı")
	}
}

func TestVerifyPassword_DotnetGeneratedHash_WrongPassword_Fails(t *testing.T) {
	if VerifyPassword("wrong-password", dotnetGoldenHash) {
		t.Fatal("yanlış şifre doğrulanmamalıydı")
	}
}

func TestHashPassword_RoundTrip_Succeeds(t *testing.T) {
	hash, err := HashPassword("correct horse battery staple")
	if err != nil {
		t.Fatalf("beklenmeyen hata: %v", err)
	}
	if !VerifyPassword("correct horse battery staple", hash) {
		t.Fatal("Go'nun ürettiği hash kendi doğrulayıcısından geçmedi")
	}
	if VerifyPassword("different", hash) {
		t.Fatal("farklı şifre doğrulanmamalıydı")
	}
}

func TestHashPassword_ProducesV3FormatMarker(t *testing.T) {
	// Go'nun yazdığı hash .NET tarafından da çözülebilmelidir (rollback güvenliği);
	// biçim işaretinin V3 (base64'te 'AQ' öneki = 0x01) olduğunu doğrularız.
	hash, err := HashPassword("x")
	if err != nil {
		t.Fatalf("beklenmeyen hata: %v", err)
	}
	if hash[:2] != "AQ" {
		t.Fatalf("V3 biçim işareti bekleniyordu, önek %q", hash[:2])
	}
}

func TestVerifyPassword_MalformedHash_Fails(t *testing.T) {
	for _, malformed := range []string{"", "not-base64!!!", "AAAA", dotnetGoldenHash[:20]} {
		if VerifyPassword("demo1234", malformed) {
			t.Fatalf("bozuk hash %q doğrulanmamalıydı", malformed)
		}
	}
}
