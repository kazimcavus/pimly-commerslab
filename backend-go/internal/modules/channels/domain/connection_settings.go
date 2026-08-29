package domain

import (
	"strings"
)

// ConnectionSettings, bağlantının kimlik bilgisi dışında kalan ayarlarıdır.
// Kimlik bilgileriyle birlikte tek bir tabloda yaşar ama ayrı bir tür olarak
// taşınır: kimlik bilgileri gizlidir ve maskelenerek döner, ayarlar ise
// kullanıcının serbestçe göreceği ve düzenleyeceği alanlardır.
type ConnectionSettings struct {
	// DisplayName, kullanıcının bağlantıyı tanıdığı addır ("Ana Mağaza").
	// Aynı pazaryerinde birden fazla bağlantı açıldığında ayırt edicidir.
	DisplayName *string

	// ExternalLocationID, stoğun yazılacağı kanal lokasyonudur. Shopify stoğu
	// lokasyon başına tutar; seçilmezse yazma ya hata verir ya yanlış depoya
	// gider. Trendyol gibi tek stoklu kanallarda boş kalır.
	ExternalLocationID *string

	// PricesIncludeVat, kanala gönderilen fiyatların KDV dahil olup olmadığıdır.
	// Türkiye pazaryerleri KDV dahil çalışır (varsayılan true); Shopify mağaza
	// ayarına göre hariç olabilir ve o durumda fiyat KDV oranı kadar sapar.
	PricesIncludeVat bool

	// ExclusionRules, mutabakatın hiç dokunmayacağı kanal kayıtlarını tanımlar.
	ExclusionRules ExclusionRules
}

// DefaultConnectionSettings, yeni bağlantının başlangıç ayarlarını döner.
func DefaultConnectionSettings() ConnectionSettings {
	return ConnectionSettings{PricesIncludeVat: true}
}

// ExclusionRules, kanal tarafındaki hangi kayıtların kapsam dışı bırakılacağını
// tanımlar. Gerçek ihtiyaç: Çağ Halı'nın 1.089 varyantlık "Özel Ölçü" kaydı —
// müşteriye özel kesim siparişleri için tutulan canlı veri, barkodsuz, PIM'de
// karşılığı yok. Ne eşleştirilmeli ne de "eksik" diye içeri alınmalı; olduğu
// yerde durmalı.
type ExclusionRules struct {
	// SkuPatterns, SQL LIKE biçiminde desenlerdir ("%-OZEL-%").
	SkuPatterns []string `json:"sku_patterns,omitempty"`

	// Statuses, kapsam dışı bırakılacak kanal durumlarıdır ("UNLISTED").
	// Karşılaştırma büyük/küçük harf duyarsızdır.
	Statuses []string `json:"statuses,omitempty"`
}

// IsExcluded, verilen kanal kaydının kapsam dışı olup olmadığını söyler.
func (r ExclusionRules) IsExcluded(sku, status string) bool {
	for _, s := range r.Statuses {
		if strings.EqualFold(strings.TrimSpace(s), strings.TrimSpace(status)) {
			return true
		}
	}
	for _, pattern := range r.SkuPatterns {
		if matchesLikePattern(sku, pattern) {
			return true
		}
	}
	return false
}

// matchesLikePattern, SQL LIKE anlamıyla eşleşme yapar: '%' herhangi bir
// diziyi, '_' tek karakteri karşılar. Büyük/küçük harf duyarsızdır — kanallar
// arasında SKU harf büyüklüğü tutarsız (Çağ Halı ölçümünde birebir eşleşme
// %0 çıkmıştı, yalnızca X/x farkından).
func matchesLikePattern(value, pattern string) bool {
	if strings.TrimSpace(pattern) == "" {
		return false
	}
	return likeMatch([]rune(strings.ToLower(value)), []rune(strings.ToLower(pattern)))
}

// likeMatch, '%' ve '_' joker karakterlerini geri izlemeli olarak eşleştirir.
func likeMatch(value, pattern []rune) bool {
	// vi/pi: geçerli konumlar. star/match: son '%' konumu ve oradaki değer
	// konumu — eşleşme tıkanırsa buraya dönülür (klasik yinelemesiz glob).
	var vi, pi, star, match int
	star = -1
	for vi < len(value) {
		switch {
		case pi < len(pattern) && (pattern[pi] == '_' || pattern[pi] == value[vi]):
			vi++
			pi++
		case pi < len(pattern) && pattern[pi] == '%':
			star = pi
			match = vi
			pi++
		case star != -1:
			pi = star + 1
			match++
			vi = match
		default:
			return false
		}
	}
	for pi < len(pattern) && pattern[pi] == '%' {
		pi++
	}
	return pi == len(pattern)
}
