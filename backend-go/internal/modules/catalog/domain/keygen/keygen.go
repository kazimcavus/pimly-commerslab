// Package keygen, özellik ve varyant anahtarlarını ad/etiketten ortak biçimde
// üretir (.NET Catalog.Domain.CatalogKeyGenerator karşılığı). Türkçe karakterler
// ASCII'ye indirgenir, harf/rakam dışı karakterler tek alt çizgiye çöker ve
// sonuç büyük harfe çevrilir: "Yaka Tipi" → "YAKA_TIPI".
package keygen

import (
	"strings"

	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// MaxLength, üretilen/açık verilen anahtarın azami uzunluğudur.
const MaxLength = 200

// turkishReplacer, küçük harfe çevrilmiş addaki Türkçe karakterleri ASCII
// karşılıklarına indirger (.NET tarafındaki Replace zinciriyle birebir).
var turkishReplacer = strings.NewReplacer("ı", "i", "ş", "s", "ğ", "g", "ü", "u", "ö", "o", "ç", "c")

// FromName, addan anahtar üretir. Hata mesajları .NET karşılığıyla aynıdır.
func FromName(name string) sharedkernel.ResultOf[string] {
	normalized := turkishReplacer.Replace(strings.ToLower(name))

	var b strings.Builder
	pendingSeparator := false
	for _, ch := range normalized {
		if isASCIILetterOrDigit(ch) {
			if pendingSeparator && b.Len() > 0 {
				b.WriteByte('_')
			}
			b.WriteRune(ch)
			pendingSeparator = false
			continue
		}
		if !pendingSeparator && b.Len() > 0 {
			pendingSeparator = true
		}
	}

	if b.Len() == 0 {
		return sharedkernel.FailOf[string](sharedkernel.NewValidationError("Key is required."))
	}
	if b.Len() > MaxLength {
		return sharedkernel.FailOf[string](sharedkernel.NewValidationError("Key must be at most 200 characters."))
	}
	return sharedkernel.OkOf(strings.ToUpper(b.String()))
}

// ValidateExplicit, kullanıcı tarafından açıkça verilen anahtarı doğrular
// (kırpar; biçim dönüşümü yapmaz).
func ValidateExplicit(value string) sharedkernel.ResultOf[string] {
	trimmed := strings.TrimSpace(value)
	if trimmed == "" {
		return sharedkernel.FailOf[string](sharedkernel.NewValidationError("Key is required."))
	}
	if len([]rune(trimmed)) > MaxLength {
		return sharedkernel.FailOf[string](sharedkernel.NewValidationError("Key must be at most 200 characters."))
	}
	return sharedkernel.OkOf(trimmed)
}

// isASCIILetterOrDigit, .NET char.IsAsciiLetterOrDigit karşılığıdır.
func isASCIILetterOrDigit(ch rune) bool {
	return (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9')
}
