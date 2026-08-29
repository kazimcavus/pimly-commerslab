package productimports

import (
	"fmt"
	"math/big"
	"sort"
	"strings"
)

// Bu dosya, pazaryerinden çekilen ürün satırlarını Pimly ürün gruplarına
// dönüştüren SAF planlayıcıdır (.NET ProductImportPlanner portu). Varyant mı
// özellik mi kararını kategori attribute tanımlarındaki IsVariant/IsSlicer
// bayraklarıyla verir; hiçbir depo/IO bağımlılığı yoktur.
//
// Kurallar:
//   - Aynı ürünün varyantları ProductMainID ile gruplanır; ModelCode = ProductMainID.
//   - IsVariant=true attribute VEYA Renk/color adlı attribute → varyant ekseni;
//     diğerleri özellik.
//   - Renk/color adlı veya IsSlicer işaretli eksen → renk seçim stili + slicer
//     (varsayılan davranış); kategori "variant" bayrağını taşımasa bile.
//   - Tek slicer: birden fazla aday varsa ilki kalır, diğerleri slicer'sız
//     devam eder (uyarı).
//   - En fazla 3 eksen: fazlası kalem düzeyi özelliğe indirgenir (uyarı).
//   - Eksen olmayan özelliklerin seviyesi TÜM satırlara bakılarak tespit edilir:
//     her satırda aynı değer → model; slicer (renk) değeri içinde sabit ama
//     renkler arasında farklı (ör. Web Renk) veya kategori tanımında IsSlicer
//     işaretli → slicer; aynı renk içinde bile farklı → kalem düzeyi.
//   - CompareAtPrice yalnızca ListPrice > SalePrice ise yazılır.

// maxVariantAxes, ürün başına desteklenen en fazla varyant ekseni sayısıdır.
const maxVariantAxes = 3

// colorNames, renk ekseni olarak tanınan özellik adlarıdır (küçük harf).
var colorNames = []string{"renk", "color", "colour"}

// skuSet, import genelinde SKU tekilliğini büyük/küçük harf duyarsız korur.
type skuSet map[string]struct{}

// add, kodu kümeye ekler; kod zaten varsa false döner.
func (s skuSet) add(code string) bool {
	key := strings.ToLower(code)
	if _, exists := s[key]; exists {
		return false
	}
	s[key] = struct{}{}
	return true
}

// BuildPlan, ürün satırlarından import planını üretir. attributeDefsByCategory,
// dış kategori kimliği → attribute tanımları eşlemesidir (cache'ten).
func BuildPlan(
	products []MarketplaceProductNode,
	attributeDefsByCategory map[string][]ProductImportAttributeDef,
) ProductImportPlan {
	// SKU tekilliği tüm import boyunca korunur (DB'de tenant başına tek SKU);
	// bir stok kodu gruplar arasında tekrar ederse yalnızca ilkine atanır,
	// diğerlerinde SKU boş kalır.
	usedSkus := skuSet{}

	rowsByMainID := map[string][]MarketplaceProductNode{}
	mainIDs := []string{}
	for _, product := range products {
		if _, seen := rowsByMainID[product.ProductMainID]; !seen {
			mainIDs = append(mainIDs, product.ProductMainID)
		}
		rowsByMainID[product.ProductMainID] = append(rowsByMainID[product.ProductMainID], product)
	}
	sort.Strings(mainIDs) // .NET OrderBy(key, Ordinal) karşılığı

	groups := make([]ProductGroupPlan, 0, len(mainIDs))
	for _, mainID := range mainIDs {
		groups = append(groups, buildGroup(mainID, rowsByMainID[mainID], attributeDefsByCategory, usedSkus))
	}
	return ProductImportPlan{Groups: groups}
}

// buildGroup, tek ProductMainID grubunun planını kurar.
func buildGroup(
	productMainID string,
	rows []MarketplaceProductNode,
	attributeDefsByCategory map[string][]ProductImportAttributeDef,
	usedSkus skuSet,
) ProductGroupPlan {
	warnings := []string{}
	first := rows[0]
	if strings.TrimSpace(first.ExternalCategoryID) == "" {
		return failedGroupPlan(productMainID, first.Title, "Ürünün pazaryeri kategorisi boş.")
	}
	externalCategoryID := first.ExternalCategoryID
	for _, row := range rows {
		if row.ExternalCategoryID != externalCategoryID {
			warnings = append(warnings, "Grup içinde farklı kategoriler var; ilk satırın kategorisi kullanıldı.")
			break
		}
	}

	defs := attributeDefsByCategory[externalCategoryID]
	defsByID := make(map[string]ProductImportAttributeDef, len(defs))
	for _, def := range defs {
		defsByID[def.ExternalAttributeID] = def
	}

	// Barkod tekilleştirme: aynı barkod tekrar ederse ilk satır esas alınır.
	uniqueRows := []MarketplaceProductNode{}
	seenBarcodes := map[string]struct{}{}
	for _, row := range rows {
		key := strings.ToLower(row.Barcode)
		if _, seen := seenBarcodes[key]; seen {
			warnings = append(warnings, fmt.Sprintf("Yinelenen barkod atlandı: %s.", row.Barcode))
			continue
		}
		seenBarcodes[key] = struct{}{}
		uniqueRows = append(uniqueRows, row)
	}

	// Varyant eksen adayları: tanımda IsVariant olan VEYA renk adlı attribute'lar.
	// Trendyol'da renk her zaman slicer'dır; kategori "variant" bayrağını
	// taşımasa bile rengi varsayılan olarak varyant eksenine alırız (kullanıcı
	// sonradan düzenleyebilir).
	axisCandidates := []PlannedVariantAxis{}
	axisCandidateSeen := map[string]struct{}{}
	for _, row := range uniqueRows {
		for _, attribute := range row.Attributes {
			def, hasDef := defsByID[attribute.ExternalAttributeID]
			if !hasDef || (!def.IsVariant && !isColorName(def.Name)) {
				continue
			}
			if _, seen := axisCandidateSeen[attribute.ExternalAttributeID]; seen {
				continue
			}
			axisCandidateSeen[attribute.ExternalAttributeID] = struct{}{}
			isColor := isColorName(def.Name)
			axisCandidates = append(axisCandidates, PlannedVariantAxis{
				ExternalAttributeID: def.ExternalAttributeID,
				Name:                def.Name,
				IsColor:             isColor,
				Slicer:              def.IsSlicer || isColor,
			})
		}
	}
	// Sıralama: slicer'lar önce, sonra renkler, sonra ad (duyarsız); kararlı sıralama
	// .NET OrderByDescending(Slicer).ThenByDescending(IsColor).ThenBy(Name) ile birebir.
	sort.SliceStable(axisCandidates, func(i, j int) bool {
		a, b := axisCandidates[i], axisCandidates[j]
		if a.Slicer != b.Slicer {
			return a.Slicer
		}
		if a.IsColor != b.IsColor {
			return a.IsColor
		}
		return strings.ToLower(a.Name) < strings.ToLower(b.Name)
	})

	// Tek slicer kuralı: ilk slicer kalır, kalanlar slicer'sız devam eder.
	slicerSeen := false
	axes := []PlannedVariantAxis{}
	for _, axis := range axisCandidates {
		if axis.Slicer && slicerSeen {
			warnings = append(warnings, fmt.Sprintf(
				"'%s' ekseni slicer olamadı; ürün başına tek slicer desteklenir.", axis.Name))
			axis.Slicer = false
			axes = append(axes, axis)
			continue
		}
		slicerSeen = slicerSeen || axis.Slicer
		axes = append(axes, axis)
	}

	// En fazla 3 eksen: fazlası kalem düzeyi özelliğe indirgenir.
	demotedAxisIDs := map[string]struct{}{}
	if len(axes) > maxVariantAxes {
		for _, demoted := range axes[maxVariantAxes:] {
			demotedAxisIDs[demoted.ExternalAttributeID] = struct{}{}
			warnings = append(warnings, fmt.Sprintf(
				"'%s' ekseni özellik olarak içe aktarıldı; en fazla %d varyant ekseni desteklenir.",
				demoted.Name, maxVariantAxes))
		}
		axes = axes[:maxVariantAxes]
	}
	axisIDs := map[string]struct{}{}
	for _, axis := range axes {
		axisIDs[axis.ExternalAttributeID] = struct{}{}
	}

	// Satır başına slicer (renk) değeri: seviye tespiti ve renk-bazlı değer
	// gruplama için.
	var groupSlicerAxisID *string
	for _, axis := range axes {
		if axis.Slicer {
			id := axis.ExternalAttributeID
			groupSlicerAxisID = &id
			break
		}
	}
	rowSlicerValue := func(row MarketplaceProductNode) *string {
		if groupSlicerAxisID == nil {
			return nil
		}
		for _, attribute := range row.Attributes {
			if attribute.ExternalAttributeID == *groupSlicerAxisID {
				return resolveValueName(attribute)
			}
		}
		return nil
	}

	// Eksen olmayan özellikler TÜM satırlara bakılarak seviyelendirilir
	// (yalnızca ilk satır değil): model (her satırda aynı), slicer (renk içinde
	// sabit, renkler arasında farklı — ör. Web Renk) veya kalem düzeyi (aynı
	// renk içinde bile farklı).
	productAttributes := []PlannedAttributeValue{}
	slicerAttributesByValue := map[string][]PlannedAttributeValue{}
	slicerAttributeValueKeys := map[string]string{} // küçük harf anahtar → özgün slicer değeri
	itemScopedAttributeIDs := map[string]struct{}{}

	nonAxisAttributeIDs := []string{}
	nonAxisSeen := map[string]struct{}{}
	for _, row := range uniqueRows {
		for _, attribute := range row.Attributes {
			id := attribute.ExternalAttributeID
			if _, isAxis := axisIDs[id]; isAxis {
				continue
			}
			if _, isDemoted := demotedAxisIDs[id]; isDemoted {
				continue
			}
			if _, seen := nonAxisSeen[id]; seen {
				continue
			}
			nonAxisSeen[id] = struct{}{}
			nonAxisAttributeIDs = append(nonAxisAttributeIDs, id)
		}
	}

	type perRowEntry struct {
		row       MarketplaceProductNode
		attribute MarketplaceProductAttributeNode
		value     string
	}
	for _, attributeID := range nonAxisAttributeIDs {
		def, hasDef := defsByID[attributeID]
		perRow := []perRowEntry{}
		for _, row := range uniqueRows {
			for _, attribute := range row.Attributes {
				if attribute.ExternalAttributeID != attributeID {
					continue
				}
				if value := resolveValueName(attribute); value != nil {
					perRow = append(perRow, perRowEntry{row: row, attribute: attribute, value: *value})
				}
				break // satırdaki ilk eşleşen attribute esas alınır (.NET FirstOrDefault)
			}
		}
		if len(perRow) == 0 {
			continue
		}
		if !hasDef {
			warnings = append(warnings, fmt.Sprintf(
				"'%s' özelliği kategori tanımında yok; yine de özellik olarak aktarıldı.",
				perRow[0].attribute.Name))
		}
		attributeName := perRow[0].attribute.Name
		required := false
		if hasDef {
			attributeName = def.Name
			required = def.Required
		}

		distinctValues := map[string]struct{}{}
		for _, entry := range perRow {
			distinctValues[strings.ToLower(entry.value)] = struct{}{}
		}

		// Kategori tanımı IsSlicer diyorsa değerler şu an tekdüze olsa bile
		// (tek renkli ürün) özellik yapısal olarak renk-bazlıdır; bayrak
		// varyans analizini ezer.
		flaggedSlicer := hasDef && def.IsSlicer && groupSlicerAxisID != nil
		if !flaggedSlicer && len(distinctValues) <= 1 {
			sample := perRow[0]
			productAttributes = append(productAttributes, PlannedAttributeValue{
				ExternalAttributeID: attributeID, AttributeName: attributeName,
				ValueName: sample.value, ExternalValueID: sample.attribute.ExternalValueID,
				Required: required, Scope: ScopeModel,
			})
			continue
		}

		// Renk (slicer değeri) içinde sabit mi? Slicer değeri okunamayan
		// satırlar analizi bozar.
		uniformWithinSlicer := groupSlicerAxisID != nil
		if uniformWithinSlicer {
			valuesBySlicer := map[string]map[string]struct{}{}
			for _, entry := range perRow {
				slicerValue := ""
				if v := rowSlicerValue(entry.row); v != nil {
					slicerValue = strings.ToLower(*v)
				}
				if valuesBySlicer[slicerValue] == nil {
					valuesBySlicer[slicerValue] = map[string]struct{}{}
				}
				valuesBySlicer[slicerValue][strings.ToLower(entry.value)] = struct{}{}
			}
			for slicerValue, values := range valuesBySlicer {
				if slicerValue == "" || len(values) > 1 {
					uniformWithinSlicer = false
					break
				}
			}
		}

		if flaggedSlicer || uniformWithinSlicer {
			// Slicer değeri başına ilk örnek değer o rengin ürününe yazılır.
			for _, entry := range perRow {
				slicerValue := rowSlicerValue(entry.row)
				if slicerValue == nil {
					continue
				}
				key := strings.ToLower(*slicerValue)
				canonical, seen := slicerAttributeValueKeys[key]
				if !seen {
					canonical = *slicerValue
					slicerAttributeValueKeys[key] = canonical
				}
				alreadyAdded := false
				for _, existing := range slicerAttributesByValue[canonical] {
					if existing.ExternalAttributeID == attributeID {
						alreadyAdded = true
						break
					}
				}
				if alreadyAdded {
					continue
				}
				slicerAttributesByValue[canonical] = append(slicerAttributesByValue[canonical], PlannedAttributeValue{
					ExternalAttributeID: attributeID, AttributeName: attributeName,
					ValueName: entry.value, ExternalValueID: entry.attribute.ExternalValueID,
					Required: required, Scope: ScopeSlicer,
				})
			}
			continue
		}
		itemScopedAttributeIDs[attributeID] = struct{}{}
	}

	// Satırlar: eksen seçimleri + indirgenen eksenlerin kalem düzeyi özellik
	// değerleri. SKU kararı sona bırakılır: önce satırlar toplanır, slicer
	// değeri başına stok kodu dağılımına bakılarak kodun renk-düzeyi mi
	// kalem-düzeyi mi olduğu anlaşılır.
	type pendingItem struct {
		row             MarketplaceProductNode
		selections      []PlannedVariantSelection
		itemAttributes  []PlannedAttributeValue
		slicerValueName *string
		stockCode       *string
	}
	pendingItems := []pendingItem{}
	for _, row := range uniqueRows {
		selections := []PlannedVariantSelection{}
		missingAxis := false
		var slicerValueName *string
		for _, axis := range axes {
			var found *MarketplaceProductAttributeNode
			for i := range row.Attributes {
				if row.Attributes[i].ExternalAttributeID == axis.ExternalAttributeID {
					found = &row.Attributes[i]
					break
				}
			}
			var valueName *string
			if found != nil {
				valueName = resolveValueName(*found)
			}
			if valueName == nil {
				missingAxis = true
				warnings = append(warnings, fmt.Sprintf(
					"Barkod %s: '%s' eksen değeri eksik.", row.Barcode, axis.Name))
				break
			}
			if groupSlicerAxisID != nil && axis.ExternalAttributeID == *groupSlicerAxisID {
				slicerValueName = valueName
			}
			selections = append(selections, PlannedVariantSelection{
				ExternalAttributeID: axis.ExternalAttributeID,
				ValueName:           *valueName,
				ExternalValueID:     found.ExternalValueID,
			})
		}
		if missingAxis {
			continue
		}

		// İndirgenen eksenler + kalem düzeyi tespit edilen özellikler kaleme yazılır.
		itemAttributes := []PlannedAttributeValue{}
		for _, attribute := range row.Attributes {
			_, isDemoted := demotedAxisIDs[attribute.ExternalAttributeID]
			_, isItemScoped := itemScopedAttributeIDs[attribute.ExternalAttributeID]
			if !isDemoted && !isItemScoped {
				continue
			}
			valueName := resolveValueName(attribute)
			if valueName == nil {
				continue
			}
			name := attribute.Name
			required := false
			if def, hasDef := defsByID[attribute.ExternalAttributeID]; hasDef {
				name = def.Name
				required = def.Required
			}
			itemAttributes = append(itemAttributes, PlannedAttributeValue{
				ExternalAttributeID: attribute.ExternalAttributeID, AttributeName: name,
				ValueName: *valueName, ExternalValueID: attribute.ExternalValueID,
				Required: required, Scope: ScopeItem,
			})
		}

		var stockCode *string
		if row.StockCode != nil && strings.TrimSpace(*row.StockCode) != "" {
			trimmed := strings.TrimSpace(*row.StockCode)
			stockCode = &trimmed
		}
		pendingItems = append(pendingItems, pendingItem{
			row: row, selections: selections, itemAttributes: itemAttributes,
			slicerValueName: slicerValueName, stockCode: stockCode,
		})
	}
	if len(pendingItems) == 0 {
		return failedGroupPlan(productMainID, first.Title,
			"Grubun hiçbir satırı içe aktarılamadı (eksen değerleri eksik veya satır yok).")
	}

	// Split planı: slicer değeri başına gerçek stok kodu (tüm kalemleri aynı
	// kodu taşıyorsa) ve o rengin orijinal listeleme başlığı. Kod, renk-düzeyi
	// ise ürünün model kodu olur; kalem SKU'suna yazılmaz (aynı kodu birden çok
	// kaleme yazmak zaten mümkün değil).
	splits := []PlannedSplit{}
	colorLevelValues := map[string]struct{}{} // küçük harf slicer değeri
	var slicerAxisID *string
	for _, axis := range axes {
		if axis.Slicer {
			id := axis.ExternalAttributeID
			slicerAxisID = &id
			break
		}
	}
	if slicerAxisID != nil {
		type valueGroup struct {
			key      string // özgün slicer değeri (ilk görülen)
			pendings []pendingItem
		}
		byValue := []valueGroup{}
		valueIndex := map[string]int{}
		for _, pending := range pendingItems {
			if pending.slicerValueName == nil {
				continue
			}
			key := strings.ToLower(*pending.slicerValueName)
			index, seen := valueIndex[key]
			if !seen {
				index = len(byValue)
				valueIndex[key] = index
				byValue = append(byValue, valueGroup{key: *pending.slicerValueName})
			}
			byValue[index].pendings = append(byValue[index].pendings, pending)
		}

		// Aynı stok kodu birden fazla renk grubunda görülüyorsa güvenilmezdir;
		// hiçbirine verilmez.
		distinctCodes := func(group valueGroup) []string {
			codes := []string{}
			seen := map[string]struct{}{}
			for _, pending := range group.pendings {
				if pending.stockCode == nil {
					continue
				}
				key := strings.ToLower(*pending.stockCode)
				if _, exists := seen[key]; exists {
					continue
				}
				seen[key] = struct{}{}
				codes = append(codes, *pending.stockCode)
			}
			return codes
		}
		codeOwners := map[string]int{}
		for _, group := range byValue {
			if codes := distinctCodes(group); len(codes) == 1 {
				codeOwners[strings.ToLower(codes[0])]++
			}
		}
		for _, group := range byValue {
			var code *string
			if codes := distinctCodes(group); len(codes) == 1 && codeOwners[strings.ToLower(codes[0])] == 1 {
				code = &codes[0]
			}
			var title, description *string
			for _, pending := range group.pendings {
				if title == nil && strings.TrimSpace(pending.row.Title) != "" {
					trimmed := strings.TrimSpace(pending.row.Title)
					title = &trimmed
				}
				if description == nil && pending.row.Description != nil && strings.TrimSpace(*pending.row.Description) != "" {
					trimmed := strings.TrimSpace(*pending.row.Description)
					description = &trimmed
				}
			}
			if code != nil {
				colorLevelValues[strings.ToLower(group.key)] = struct{}{}
			}
			splits = append(splits, PlannedSplit{
				ValueName: group.key, StockCode: code, Title: title, Description: description,
				SplitAttributeValues: slicerAttributesByValue[canonicalSlicerKey(slicerAttributeValueKeys, group.key)],
			})
		}
	}

	items := make([]PlannedItem, 0, len(pendingItems))
	for _, pending := range pendingItems {
		isColorLevelCode := false
		if pending.slicerValueName != nil {
			_, isColorLevelCode = colorLevelValues[strings.ToLower(*pending.slicerValueName)]
		}
		sku := deriveItemSku(pending.stockCode, pending.selections, isColorLevelCode, slicerAxisID, usedSkus)

		var compareAt *string
		if decimalGreaterThan(pending.row.ListPrice, pending.row.SalePrice) {
			listPrice := pending.row.ListPrice
			compareAt = &listPrice
		}
		var currency *string
		if pending.row.CurrencyType != nil && strings.TrimSpace(*pending.row.CurrencyType) != "" {
			trimmed := strings.TrimSpace(*pending.row.CurrencyType)
			currency = &trimmed
		}
		stock := pending.row.Quantity
		if stock < 0 {
			stock = 0
		}
		items = append(items, PlannedItem{
			Barcode: pending.row.Barcode, Sku: sku,
			Price: pending.row.SalePrice, CompareAtPrice: compareAt,
			Stock: stock, Currency: currency,
			VariantSelections: pending.selections, ItemAttributeValues: pending.itemAttributes,
			ImageURLs: pending.row.ImageURLs,
		})
	}

	return ProductGroupPlan{
		ProductMainID: productMainID, Name: first.Title,
		ExternalCategoryID: externalCategoryID, ModelCode: productMainID,
		VariantAxes: axes, AttributeValues: productAttributes, Items: items,
		Warnings: warnings, SplitOverrides: splits,
		BrandName:       trimToNilPtr(first.Brand),
		BrandExternalID: trimToNilPtr(first.BrandExternalID),
		Description:     trimToNilPtr(first.Description),
	}
}

// canonicalSlicerKey, slicer değerinin renk-bazlı özellik listesinde kullanılan
// özgün anahtarını döner (büyük/küçük harf farkları tek anahtara iner).
func canonicalSlicerKey(keys map[string]string, value string) string {
	if canonical, ok := keys[strings.ToLower(value)]; ok {
		return canonical
	}
	return value
}

// deriveItemSku, kalem SKU'sunu türetir: kalem-düzeyi stok kodu import
// genelinde tekilse doğrudan kullanılır; renk-düzeyi kod (aynı kod tüm
// bedenlerde) slicer-dışı eksen değeriyle (ör. beden) benzersizleştirilir →
// "26AKR0009R05-80X150". Tekilleştirilemezse veya 200 karakteri aşarsa nil
// döner (çakışan SKU asla yazılmaz).
func deriveItemSku(
	stockCode *string,
	selections []PlannedVariantSelection,
	isColorLevelCode bool,
	slicerAxisID *string,
	usedSkus skuSet,
) *string {
	if stockCode == nil {
		return nil
	}
	if !isColorLevelCode {
		if usedSkus.add(*stockCode) {
			return stockCode
		}
		return nil
	}
	var suffix strings.Builder
	for _, selection := range selections {
		if slicerAxisID != nil && selection.ExternalAttributeID == *slicerAxisID {
			continue
		}
		suffix.WriteString(normalizeSkuToken(selection.ValueName))
	}
	if suffix.Len() == 0 {
		return nil
	}
	candidate := *stockCode + "-" + suffix.String()
	if len([]rune(candidate)) > 200 || !usedSkus.add(candidate) {
		return nil
	}
	return &candidate
}

// normalizeSkuToken, değerdeki boşlukları atıp büyük harfe çevirir
// (.NET NormalizeSkuToken portu).
func normalizeSkuToken(value string) string {
	var b strings.Builder
	for _, ch := range value {
		if ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r' || ch == '\f' || ch == '\v' {
			continue
		}
		b.WriteRune(ch)
	}
	return strings.ToUpper(b.String())
}

// isColorName, özellik adının renk ekseni sayılıp sayılmadığını döner.
func isColorName(name string) bool {
	normalized := strings.ToLower(strings.TrimSpace(name))
	for _, candidate := range colorNames {
		if normalized == candidate {
			return true
		}
	}
	return false
}

// resolveValueName, özellik düğümünün etkin değer adını çözer: önce sözlük
// değeri, yoksa serbest metin; ikisi de boşsa nil (.NET ResolveValueName portu).
func resolveValueName(attribute MarketplaceProductAttributeNode) *string {
	if attribute.Value != nil && strings.TrimSpace(*attribute.Value) != "" {
		trimmed := strings.TrimSpace(*attribute.Value)
		return &trimmed
	}
	if attribute.CustomValue != nil && strings.TrimSpace(*attribute.CustomValue) != "" {
		trimmed := strings.TrimSpace(*attribute.CustomValue)
		return &trimmed
	}
	return nil
}

// decimalGreaterThan, iki ham ondalık dizgiyi kayıpsız karşılaştırır; herhangi
// biri çözülemezse false döner.
func decimalGreaterThan(a, b string) bool {
	ra, okA := new(big.Rat).SetString(strings.TrimSpace(a))
	rb, okB := new(big.Rat).SetString(strings.TrimSpace(b))
	return okA && okB && ra.Cmp(rb) > 0
}

// trimToNilPtr, kırpılmış değeri döner; boşsa nil.
func trimToNilPtr(value *string) *string {
	if value == nil {
		return nil
	}
	trimmed := strings.TrimSpace(*value)
	if trimmed == "" {
		return nil
	}
	return &trimmed
}
