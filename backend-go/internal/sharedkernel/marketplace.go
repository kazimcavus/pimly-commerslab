package sharedkernel

import "strings"

// Marketplace, desteklenen pazaryerlerini temsil eden kapalı kümeli değer
// nesnesidir. .NET SharedKernel.Marketplace karşılığı; bugün tek üye vardır:
// Trendyol ("TY"). Yeni pazaryeri eklerken hem kod sabiti hem de
// marketplaceRegistry güncellenmelidir.
type Marketplace struct {
	code string
	name string
}

// MarketplaceCodeTrendyol, Trendyol pazaryerinin kablo formatındaki kodudur.
const MarketplaceCodeTrendyol = "TY"

// MarketplaceTrendyol, Trendyol pazaryeri değeridir.
var MarketplaceTrendyol = Marketplace{code: MarketplaceCodeTrendyol, name: "Trendyol"}

// marketplaceRegistry, koddan pazaryerine kapalı eşlemedir.
var marketplaceRegistry = map[string]Marketplace{
	MarketplaceCodeTrendyol: MarketplaceTrendyol,
}

// Code, pazaryerinin kablo formatındaki kodunu döner (ör. "TY").
func (m Marketplace) Code() string { return m.code }

// Name, pazaryerinin okunur adını döner (ör. "Trendyol").
func (m Marketplace) Name() string { return m.name }

// IsZero, değerin doldurulmamış (geçersiz) olup olmadığını döner.
func (m Marketplace) IsZero() bool { return m.code == "" }

// MarketplaceFromCode, kullanıcı girdisinden pazaryeri çözer
// (.NET Marketplace.FromCode karşılığı): kod normalize edilir (kırp + büyük
// harf), boş kod doğrulama hatası, bilinmeyen kod not_found üretir.
func MarketplaceFromCode(code string) ResultOf[Marketplace] {
	if strings.TrimSpace(code) == "" {
		return FailOf[Marketplace](NewValidationError("Marketplace code is required."))
	}
	if m, ok := marketplaceRegistry[strings.ToUpper(strings.TrimSpace(code))]; ok {
		return OkOf(m)
	}
	return FailOf[Marketplace](NewNotFoundError("Marketplace not found."))
}

// MarketplaceFromPersistence, veritabanından okunan kodu çözer; bilinmeyen kod
// veri bütünlüğü hatası olduğundan panic üretir (.NET FromPersistence throw karşılığı).
func MarketplaceFromPersistence(code string) Marketplace {
	m, ok := marketplaceRegistry[code]
	if !ok {
		panic("sharedkernel: veritabanında bilinmeyen pazaryeri kodu: " + code)
	}
	return m
}
