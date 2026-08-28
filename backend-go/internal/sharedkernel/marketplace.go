package sharedkernel

import "fmt"

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

// MarketplaceFromCode, kullanıcı girdisinden pazaryeri çözer; bilinmeyen kod
// doğrulama hatası üretir (.NET Marketplace.FromCode karşılığı).
func MarketplaceFromCode(code string) ResultOf[Marketplace] {
	if m, ok := marketplaceRegistry[code]; ok {
		return OkOf(m)
	}
	return FailOf[Marketplace](NewValidationError(
		fmt.Sprintf("Unknown marketplace code '%s'.", code)))
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
