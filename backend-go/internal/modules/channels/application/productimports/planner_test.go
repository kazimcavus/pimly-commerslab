package productimports

// ProductImportPlanner birim testleri (.NET ProductImportPlannerTests portu):
// varyant/özellik sınıflandırması, slicer ve Renk sezgiseli, eksen sınırı,
// fiyat/stok eşlemesi ve renk-düzeyi stok kodu kuralları.
//
// Not: .NET'teki BuildPlan_ColorLevelStockCode_BecomesSplitCodeAndClearsItemSkus
// testi ESKİ planner davranışına (renk-düzeyi kodda SKU boş) göre yazılmıştı;
// güncel planner renk-düzeyi kodu slicer-dışı eksen değeriyle benzersizleştirip
// "KOD-BEDEN" SKU'su türetir. Buradaki test güncel davranışı doğrular.

import (
	"strings"
	"testing"
)

// strPtr, dizgi işaretçisi kısayoludur.
func strPtr(value string) *string { return &value }

// gomlekDefs, testlerde kullanılan varsayılan kategori tanımlarıdır.
func gomlekDefs() []ProductImportAttributeDef {
	return []ProductImportAttributeDef{
		{ExternalAttributeID: "attr-renk", Name: "Renk", Required: true, IsVariant: true, IsSlicer: true},
		{ExternalAttributeID: "attr-beden", Name: "Beden", Required: true, IsVariant: true, IsSlicer: false},
		{ExternalAttributeID: "attr-kumas", Name: "Kumaş", Required: true, IsVariant: false, IsSlicer: false},
	}
}

// defs, "221" kategorisi için varsayılan tanım haritasını döner.
func defs() map[string][]ProductImportAttributeDef {
	return map[string][]ProductImportAttributeDef{"221": gomlekDefs()}
}

// rowOptions, row üreticisinin geçersiz kılınabilir alanlarıdır.
type rowOptions struct {
	mainID    string
	renk      string
	renkID    *string
	beden     string
	bedenID   *string
	listPrice string
	salePrice string
	quantity  int
	category  string
	stockCode *string
	title     string
}

// row, .NET testlerindeki Row() üreticisinin karşılığıdır.
func row(barcode string, mutate func(*rowOptions)) MarketplaceProductNode {
	options := rowOptions{
		mainID: "GOMLEK-001", renk: "Mavi", renkID: strPtr("val-mavi"),
		beden: "S", bedenID: strPtr("val-s"),
		listPrice: "599.90", salePrice: "449.90", quantity: 10,
		category: "221", title: "Klasik Gömlek",
	}
	if mutate != nil {
		mutate(&options)
	}
	stockCode := options.stockCode
	if stockCode == nil {
		stockCode = strPtr("STK-" + barcode)
	}
	return MarketplaceProductNode{
		Barcode: barcode, Title: options.title, ProductMainID: options.mainID,
		Brand: strPtr("Pimly"), StockCode: stockCode, Quantity: options.quantity,
		ListPrice: options.listPrice, SalePrice: options.salePrice,
		CurrencyType: strPtr("TRY"), ExternalCategoryID: options.category,
		CategoryName: strPtr("Gömlek"), Description: strPtr("Açıklama"), Approved: true,
		Attributes: []MarketplaceProductAttributeNode{
			{ExternalAttributeID: "attr-renk", Name: "Renk", ExternalValueID: options.renkID, Value: strPtr(options.renk)},
			{ExternalAttributeID: "attr-beden", Name: "Beden", ExternalValueID: options.bedenID, Value: strPtr(options.beden)},
			{ExternalAttributeID: "attr-kumas", Name: "Kumaş", ExternalValueID: strPtr("val-pamuk"), Value: strPtr("Pamuk")},
		},
	}
}

// makeRow, özel attribute listesiyle satır üretir (.NET MakeRow karşılığı).
func makeRow(barcode, mainID, categoryID string, attributes []MarketplaceProductAttributeNode) MarketplaceProductNode {
	return MarketplaceProductNode{
		Barcode: barcode, Title: "Ürün", ProductMainID: mainID,
		Quantity: 1, ListPrice: "100", SalePrice: "100",
		CurrencyType: strPtr("TRY"), ExternalCategoryID: categoryID,
		Approved: true, Attributes: attributes,
	}
}

// findGroup, plan içinde ProductMainID ile grup arar.
func findGroup(t *testing.T, plan ProductImportPlan, mainID string) ProductGroupPlan {
	t.Helper()
	for _, group := range plan.Groups {
		if group.ProductMainID == mainID {
			return group
		}
	}
	t.Fatalf("grup bulunamadı: %s", mainID)
	return ProductGroupPlan{}
}

// containsWarning, uyarılar içinde parça arar.
func containsWarning(warnings []string, fragment string) bool {
	for _, warning := range warnings {
		if strings.Contains(strings.ToLower(warning), strings.ToLower(fragment)) {
			return true
		}
	}
	return false
}

func TestBuildPlan_GroupsByProductMainID(t *testing.T) {
	plan := BuildPlan([]MarketplaceProductNode{
		row("1", nil),
		row("2", func(o *rowOptions) { o.beden = "M"; o.bedenID = strPtr("val-m") }),
		row("3", func(o *rowOptions) { o.mainID = "GOMLEK-002" }),
	}, defs())

	if len(plan.Groups) != 2 {
		t.Fatalf("2 grup bekleniyordu, %d bulundu", len(plan.Groups))
	}
	first := findGroup(t, plan, "GOMLEK-001")
	if len(first.Items) != 2 {
		t.Fatalf("GOMLEK-001 için 2 kalem bekleniyordu, %d bulundu", len(first.Items))
	}
	findGroup(t, plan, "GOMLEK-002")
}

func TestBuildPlan_ClassifiesVariantsAndAttributes(t *testing.T) {
	plan := BuildPlan([]MarketplaceProductNode{row("1", nil)}, defs())
	group := plan.Groups[0]

	names := map[string]bool{}
	for _, axis := range group.VariantAxes {
		names[axis.Name] = true
	}
	if !names["Renk"] || !names["Beden"] || len(group.VariantAxes) != 2 {
		t.Fatalf("eksenler [Renk Beden] bekleniyordu: %+v", group.VariantAxes)
	}
	found := false
	for _, attribute := range group.AttributeValues {
		if attribute.AttributeName == "Kumaş" && attribute.ValueName == "Pamuk" {
			found = true
		}
	}
	if !found {
		t.Fatalf("Kumaş=Pamuk model özelliği bekleniyordu: %+v", group.AttributeValues)
	}
}

func TestBuildPlan_ColorAxisIsSlicerAndColorStyle(t *testing.T) {
	plan := BuildPlan([]MarketplaceProductNode{row("1", nil)}, defs())
	for _, axis := range plan.Groups[0].VariantAxes {
		switch axis.Name {
		case "Renk":
			if !axis.IsColor || !axis.Slicer {
				t.Fatalf("Renk ekseni color+slicer olmalı: %+v", axis)
			}
		case "Beden":
			if axis.Slicer {
				t.Fatalf("Beden ekseni slicer olmamalı: %+v", axis)
			}
		}
	}
}

func TestBuildPlan_ColorNameHeuristic_MakesRenkSlicerEvenWithoutFlag(t *testing.T) {
	// Kategori tanımında IsSlicer yok ama ad "Renk" → varsayılan slicer davranışı.
	customDefs := map[string][]ProductImportAttributeDef{"221": {
		{ExternalAttributeID: "attr-renk", Name: "Renk", Required: true, IsVariant: true},
		{ExternalAttributeID: "attr-beden", Name: "Beden", Required: true, IsVariant: true},
	}}
	plan := BuildPlan([]MarketplaceProductNode{row("1", nil)}, customDefs)
	for _, axis := range plan.Groups[0].VariantAxes {
		if axis.Name == "Renk" && !axis.Slicer {
			t.Fatal("Renk ekseni ad sezgiseliyle slicer olmalıydı")
		}
	}
}

func TestBuildPlan_ColorBecomesVariantAxis_EvenWhenNotFlaggedVariant(t *testing.T) {
	// Trendyol kategorisi rengi "variant" işaretlemese bile (IsVariant=false),
	// renk varsayılan olarak varyant ekseni + slicer olmalı; özelliğe düşmemeli.
	customDefs := map[string][]ProductImportAttributeDef{"221": {
		{ExternalAttributeID: "attr-renk", Name: "Renk", Required: true},
		{ExternalAttributeID: "attr-beden", Name: "Beden", Required: true, IsVariant: true},
		{ExternalAttributeID: "attr-kumas", Name: "Kumaş", Required: true},
	}}
	group := BuildPlan([]MarketplaceProductNode{row("1", nil)}, customDefs).Groups[0]

	renkFound := false
	for _, axis := range group.VariantAxes {
		if axis.Name == "Renk" {
			renkFound = true
			if !axis.IsColor || !axis.Slicer {
				t.Fatalf("Renk ekseni color+slicer olmalı: %+v", axis)
			}
		}
	}
	if !renkFound {
		t.Fatal("Renk varyant ekseni olmalıydı")
	}
	for _, attribute := range group.AttributeValues {
		if attribute.AttributeName == "Renk" {
			t.Fatal("Renk model özelliğine düşmemeliydi")
		}
	}
}

func TestBuildPlan_OnlyOneSlicerSurvives(t *testing.T) {
	customDefs := map[string][]ProductImportAttributeDef{"221": {
		{ExternalAttributeID: "attr-renk", Name: "Renk", Required: true, IsVariant: true, IsSlicer: true},
		{ExternalAttributeID: "attr-beden", Name: "Beden", Required: true, IsVariant: true, IsSlicer: true},
	}}
	group := BuildPlan([]MarketplaceProductNode{row("1", nil)}, customDefs).Groups[0]

	slicerCount := 0
	for _, axis := range group.VariantAxes {
		if axis.Slicer {
			slicerCount++
		}
	}
	if slicerCount != 1 {
		t.Fatalf("tek slicer bekleniyordu, %d bulundu", slicerCount)
	}
	if !containsWarning(group.Warnings, "slicer") {
		t.Fatalf("slicer uyarısı bekleniyordu: %+v", group.Warnings)
	}
}

func TestBuildPlan_MoreThanThreeAxes_DemotesExtrasToItemAttributes(t *testing.T) {
	customDefs := map[string][]ProductImportAttributeDef{"221": {
		{ExternalAttributeID: "a1", Name: "Renk", Required: true, IsVariant: true, IsSlicer: true},
		{ExternalAttributeID: "a2", Name: "Beden", Required: true, IsVariant: true},
		{ExternalAttributeID: "a3", Name: "Numara", Required: true, IsVariant: true},
		{ExternalAttributeID: "a4", Name: "Kalıp", Required: true, IsVariant: true},
	}}
	testRow := makeRow("1", "MAIN-1", "221", []MarketplaceProductAttributeNode{
		{ExternalAttributeID: "a1", Name: "Renk", ExternalValueID: strPtr("v1"), Value: strPtr("Mavi")},
		{ExternalAttributeID: "a2", Name: "Beden", ExternalValueID: strPtr("v2"), Value: strPtr("S")},
		{ExternalAttributeID: "a3", Name: "Numara", ExternalValueID: strPtr("v3"), Value: strPtr("42")},
		{ExternalAttributeID: "a4", Name: "Kalıp", ExternalValueID: strPtr("v4"), Value: strPtr("Slim")},
	})
	group := BuildPlan([]MarketplaceProductNode{testRow}, customDefs).Groups[0]

	if len(group.VariantAxes) != 3 {
		t.Fatalf("3 eksen bekleniyordu, %d bulundu", len(group.VariantAxes))
	}
	// Eksen sıralaması: slicer > renk > ada göre — alfabetik son kalan "Numara" indirgenir.
	numaraFound := false
	for _, attribute := range group.Items[0].ItemAttributeValues {
		if attribute.AttributeName == "Numara" {
			numaraFound = true
		}
	}
	if !numaraFound {
		t.Fatalf("Numara kalem özelliğine indirgenmiş olmalıydı: %+v", group.Items[0].ItemAttributeValues)
	}
	if !containsWarning(group.Warnings, "Numara") {
		t.Fatalf("Numara uyarısı bekleniyordu: %+v", group.Warnings)
	}
}

func TestBuildPlan_CompareAtOnlyWhenListPriceHigher(t *testing.T) {
	plan := BuildPlan([]MarketplaceProductNode{
		row("1", nil), // 599.90 > 449.90
		row("2", func(o *rowOptions) {
			o.beden = "M"
			o.bedenID = strPtr("val-m")
			o.listPrice = "449.90"
		}),
	}, defs())
	items := plan.Groups[0].Items

	for _, item := range items {
		switch item.Barcode {
		case "1":
			if item.CompareAtPrice == nil || *item.CompareAtPrice != "599.90" {
				t.Fatalf("barkod 1 için CompareAtPrice 599.90 bekleniyordu: %+v", item.CompareAtPrice)
			}
			if item.Price != "449.90" {
				t.Fatalf("barkod 1 için Price 449.90 bekleniyordu: %s", item.Price)
			}
		case "2":
			if item.CompareAtPrice != nil {
				t.Fatalf("barkod 2 için CompareAtPrice nil bekleniyordu: %v", *item.CompareAtPrice)
			}
		}
	}
}

func TestBuildPlan_DuplicateBarcode_KeepsFirstWithWarning(t *testing.T) {
	plan := BuildPlan([]MarketplaceProductNode{
		row("1", nil),
		row("1", func(o *rowOptions) { o.beden = "M"; o.bedenID = strPtr("val-m") }),
	}, defs())
	group := plan.Groups[0]

	if len(group.Items) != 1 {
		t.Fatalf("1 kalem bekleniyordu, %d bulundu", len(group.Items))
	}
	if !containsWarning(group.Warnings, "Yinelenen barkod") {
		t.Fatalf("yinelenen barkod uyarısı bekleniyordu: %+v", group.Warnings)
	}
}

func TestBuildPlan_MissingAxisValue_SkipsRow(t *testing.T) {
	broken := makeRow("9", "GOMLEK-001", "221", []MarketplaceProductAttributeNode{
		{ExternalAttributeID: "attr-kumas", Name: "Kumaş", ExternalValueID: strPtr("val-pamuk"), Value: strPtr("Pamuk")},
	})
	plan := BuildPlan([]MarketplaceProductNode{row("1", nil), broken}, defs())
	group := plan.Groups[0]

	if len(group.Items) != 1 || group.Items[0].Barcode != "1" {
		t.Fatalf("yalnızca barkod 1 kalmalıydı: %+v", group.Items)
	}
	if !containsWarning(group.Warnings, "9") {
		t.Fatalf("eksik eksen uyarısı bekleniyordu: %+v", group.Warnings)
	}
}

func TestBuildPlan_EmptyCategory_FailsGroup(t *testing.T) {
	testRow := makeRow("1", "MAIN-1", "", nil)
	plan := BuildPlan([]MarketplaceProductNode{testRow}, defs())
	if plan.Groups[0].Error == nil {
		t.Fatal("boş kategoride grup hatası bekleniyordu")
	}
}

func TestBuildPlan_CustomAttributeValue_UsedWhenValueMissing(t *testing.T) {
	testRow := makeRow("1", "MAIN-1", "221", []MarketplaceProductAttributeNode{
		{ExternalAttributeID: "attr-kumas", Name: "Kumaş", CustomValue: strPtr("Keten")},
	})
	group := BuildPlan([]MarketplaceProductNode{testRow}, defs()).Groups[0]

	found := false
	for _, attribute := range group.AttributeValues {
		if attribute.AttributeName == "Kumaş" && attribute.ValueName == "Keten" && attribute.ExternalValueID == nil {
			found = true
		}
	}
	if !found {
		t.Fatalf("Kumaş=Keten (serbest metin) bekleniyordu: %+v", group.AttributeValues)
	}
}

func TestBuildPlan_NegativeStock_ClampedToZero(t *testing.T) {
	plan := BuildPlan([]MarketplaceProductNode{
		row("1", func(o *rowOptions) { o.quantity = -3 }),
	}, defs())
	if stock := plan.Groups[0].Items[0].Stock; stock != 0 {
		t.Fatalf("stok 0'a sabitlenmeliydi, %d bulundu", stock)
	}
}

func TestBuildPlan_ColorLevelStockCode_BecomesSplitCodeAndDerivesSuffixedSkus(t *testing.T) {
	// Trendyol'da stok kodu çoğu zaman renk düzeyindedir: aynı rengin tüm
	// bedenleri aynı kodu taşır. Kod split'e (renk ürününün model kodu) taşınır;
	// kalem SKU'ları slicer-dışı eksen değeriyle benzersizleştirilir (KOD-BEDEN).
	plan := BuildPlan([]MarketplaceProductNode{
		row("1", func(o *rowOptions) {
			o.renk = "Vizon"
			o.renkID = strPtr("val-vizon")
			o.stockCode = strPtr("25CSM02817NR03")
			o.title = "Vizon Klasik Halı"
		}),
		row("2", func(o *rowOptions) {
			o.renk = "Vizon"
			o.renkID = strPtr("val-vizon")
			o.beden = "M"
			o.bedenID = strPtr("val-m")
			o.stockCode = strPtr("25CSM02817NR03")
			o.title = "Vizon Klasik Halı"
		}),
		row("3", func(o *rowOptions) {
			o.renk = "Bej"
			o.renkID = strPtr("val-bej")
			o.stockCode = strPtr("25CSM02817CR03")
			o.title = "Bej Cashmira Halı"
		}),
	}, defs())
	group := plan.Groups[0]

	if len(group.SplitOverrides) != 2 {
		t.Fatalf("2 split bekleniyordu, %d bulundu", len(group.SplitOverrides))
	}
	for _, split := range group.SplitOverrides {
		switch split.ValueName {
		case "Vizon":
			if split.StockCode == nil || *split.StockCode != "25CSM02817NR03" {
				t.Fatalf("Vizon split kodu 25CSM02817NR03 olmalıydı: %+v", split.StockCode)
			}
			if split.Title == nil || *split.Title != "Vizon Klasik Halı" {
				t.Fatalf("Vizon split başlığı korunmalıydı: %+v", split.Title)
			}
		case "Bej":
			if split.StockCode == nil || *split.StockCode != "25CSM02817CR03" {
				t.Fatalf("Bej split kodu 25CSM02817CR03 olmalıydı: %+v", split.StockCode)
			}
		default:
			t.Fatalf("beklenmeyen split: %s", split.ValueName)
		}
	}
	expectedSkus := map[string]string{
		"1": "25CSM02817NR03-S", "2": "25CSM02817NR03-M", "3": "25CSM02817CR03-S"}
	for _, item := range group.Items {
		expected := expectedSkus[item.Barcode]
		if item.Sku == nil || *item.Sku != expected {
			t.Fatalf("barkod %s için SKU %q bekleniyordu: %+v", item.Barcode, expected, item.Sku)
		}
	}
}

func TestBuildPlan_ItemLevelStockCodes_KeepItemSkus_AndSplitCodeStaysEmpty(t *testing.T) {
	// Renk içinde kalem başına farklı stok kodları → kod kalem düzeyindedir;
	// split koduna taşınmaz, SKU'lar eski kuralla kalemlere yazılır.
	plan := BuildPlan([]MarketplaceProductNode{
		row("1", func(o *rowOptions) { o.renk = "Vizon"; o.renkID = strPtr("val-vizon") }),
		row("2", func(o *rowOptions) {
			o.renk = "Vizon"
			o.renkID = strPtr("val-vizon")
			o.beden = "M"
			o.bedenID = strPtr("val-m")
		}),
	}, defs())
	group := plan.Groups[0]

	for _, split := range group.SplitOverrides {
		if split.ValueName == "Vizon" && split.StockCode != nil {
			t.Fatalf("Vizon split kodu boş kalmalıydı: %v", *split.StockCode)
		}
	}
	expected := map[string]string{"1": "STK-1", "2": "STK-2"}
	for _, item := range group.Items {
		if item.Sku == nil || *item.Sku != expected[item.Barcode] {
			t.Fatalf("barkod %s için SKU %q bekleniyordu: %+v", item.Barcode, expected[item.Barcode], item.Sku)
		}
	}
}

func TestBuildPlan_SameStockCodeAcrossColors_NotUsedAsSplitCode(t *testing.T) {
	// Aynı kod iki renkte görülüyorsa güvenilmezdir; hiçbir renge verilmez.
	plan := BuildPlan([]MarketplaceProductNode{
		row("1", func(o *rowOptions) {
			o.renk = "Vizon"
			o.renkID = strPtr("val-vizon")
			o.stockCode = strPtr("SHARED")
		}),
		row("2", func(o *rowOptions) {
			o.renk = "Bej"
			o.renkID = strPtr("val-bej")
			o.stockCode = strPtr("SHARED")
		}),
	}, defs())
	for _, split := range plan.Groups[0].SplitOverrides {
		if split.StockCode != nil {
			t.Fatalf("split kodu boş kalmalıydı: %s → %v", split.ValueName, *split.StockCode)
		}
	}
}

func TestBuildPlan_SkuUniqueness_SecondUseOfSameCodeDropsSku(t *testing.T) {
	// Aynı stok kodu farklı gruplarda tekrar ederse yalnızca ilkine atanır
	// (DB'de tenant başına tek SKU).
	plan := BuildPlan([]MarketplaceProductNode{
		row("1", func(o *rowOptions) { o.stockCode = strPtr("DUP-CODE") }),
		row("2", func(o *rowOptions) {
			o.mainID = "GOMLEK-002"
			o.stockCode = strPtr("DUP-CODE")
		}),
	}, defs())

	// Tek renkli grupta kod renk-düzeyi sayılır: SKU slicer-dışı eksen değeriyle
	// benzersizleştirilir (DUP-CODE-S). İkinci grupta aynı aday tekrar üretilir
	// ama küme onu zaten içerdiğinden SKU boş kalır.
	first := findGroup(t, plan, "GOMLEK-001")
	second := findGroup(t, plan, "GOMLEK-002")
	if first.Items[0].Sku == nil || *first.Items[0].Sku != "DUP-CODE-S" {
		t.Fatalf("ilk kullanım DUP-CODE-S almalıydı: %+v", first.Items[0].Sku)
	}
	if second.Items[0].Sku != nil {
		t.Fatalf("ikinci kullanım SKU almamalıydı: %v", *second.Items[0].Sku)
	}
}

func TestBuildPlan_SlicerScopedAttribute_GroupedBySlicerValue(t *testing.T) {
	// Renk içinde sabit ama renkler arasında farklı özellik (ör. Web Renk) →
	// slicer seviyesine yazılır ve split'in özellik listesine girer.
	customDefs := map[string][]ProductImportAttributeDef{"221": {
		{ExternalAttributeID: "attr-renk", Name: "Renk", Required: true, IsVariant: true, IsSlicer: true},
		{ExternalAttributeID: "attr-web", Name: "Web Renk", Required: false},
	}}
	build := func(barcode, renk, renkID, webRenk string) MarketplaceProductNode {
		return makeRow(barcode, "MAIN-1", "221", []MarketplaceProductAttributeNode{
			{ExternalAttributeID: "attr-renk", Name: "Renk", ExternalValueID: strPtr(renkID), Value: strPtr(renk)},
			{ExternalAttributeID: "attr-web", Name: "Web Renk", Value: strPtr(webRenk)},
		})
	}
	plan := BuildPlan([]MarketplaceProductNode{
		build("1", "Vizon", "v1", "Krem"),
		build("2", "Bej", "v2", "Kum"),
	}, customDefs)
	group := plan.Groups[0]

	for _, attribute := range group.AttributeValues {
		if attribute.AttributeName == "Web Renk" {
			t.Fatal("Web Renk model seviyesine yazılmamalıydı")
		}
	}
	seen := map[string]string{}
	for _, split := range group.SplitOverrides {
		for _, attribute := range split.SplitAttributeValues {
			if attribute.AttributeName == "Web Renk" {
				seen[split.ValueName] = attribute.ValueName
				if attribute.Scope != ScopeSlicer {
					t.Fatalf("Web Renk slicer seviyesinde olmalıydı: %v", attribute.Scope)
				}
			}
		}
	}
	if seen["Vizon"] != "Krem" || seen["Bej"] != "Kum" {
		t.Fatalf("split özellik değerleri beklenenden farklı: %+v", seen)
	}
}
