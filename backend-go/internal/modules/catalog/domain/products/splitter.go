package products

import (
	"strings"
	"unicode"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// CreatePlan, slicer eksenine göre bölünmüş tek ürün oluşturma planıdır
// (.NET ProductCreatePlan). GroupCode her planda paylaşılan temel koddur;
// SlicerValue bölünen eksen değeridir (bölünmemişse nil).
type CreatePlan struct {
	ModelCode   string
	Name        string
	Variants    []VariantRef
	Items       []ItemDraft
	GroupCode   *string
	SlicerValue *string
	Description *string
}

// SplitOverride, slicer değerine özel plan geçersiz kılmalarıdır: pazaryeri
// import'unda renk ürününün gerçek stok kodu, orijinal başlığı ve açıklaması
// buradan taşınır; verilmeyen alanlar için türetilmiş varsayılanlar kullanılır.
type SplitOverride struct {
	ValueName   string
	ModelCode   *string
	Name        *string
	Description *string
}

// Split, ürün oluşturma girdisini slicer varyant türüne göre bir veya birden
// fazla plana böler (.NET ProductCreateSplitter.Split portu). Renk slicer ise
// "GOMlek-001" girdisi renk başına ayrı planlara ayrılır; slicer dışı eksen
// yoksa slicer ekseni planda korunur.
func Split(baseModelCode, baseName string, variants []VariantRef, items []ItemDraft, overrides []SplitOverride) sharedkernel.ResultOf[[]CreatePlan] {
	var slicerTypes []VariantRef
	for _, v := range variants {
		if v.Slicer {
			slicerTypes = append(slicerTypes, v)
		}
	}
	if len(slicerTypes) == 0 {
		return sharedkernel.OkOf([]CreatePlan{{
			ModelCode: baseModelCode, Name: strings.TrimSpace(baseName), Variants: variants, Items: items}})
	}
	if len(slicerTypes) > 1 {
		return sharedkernel.FailOf[[]CreatePlan](
			sharedkernel.NewValidationError("Only one slicer variant type is allowed per product."))
	}
	if len(items) == 0 {
		return sharedkernel.FailOf[[]CreatePlan](
			sharedkernel.NewValidationError("At least one item is required."))
	}

	slicerType := slicerTypes[0]
	var remainingTypes []VariantRef
	for _, v := range variants {
		if !v.Slicer {
			remainingTypes = append(remainingTypes, v)
		}
	}
	stripSlicer := len(remainingTypes) > 0

	// Slicer değerine göre gruplar; ekleme sırası korunur, planlar değere göre
	// büyük/küçük harf duyarsız alfabetik sıralanır (.NET OrderBy davranışı).
	type slicerGroup struct {
		selection VariantValue
		items     []ItemDraft
	}
	groups := map[uuid.UUID]*slicerGroup{}
	var order []uuid.UUID
	for _, item := range items {
		var selection *VariantValue
		for i := range item.VariantValues {
			if item.VariantValues[i].Variant.ID == slicerType.ID {
				selection = &item.VariantValues[i]
				break
			}
		}
		if selection == nil {
			return sharedkernel.FailOf[[]CreatePlan](sharedkernel.NewValidationError(
				"Each item must include a selection for slicer type '" + slicerType.Name + "'."))
		}
		group, ok := groups[selection.ID]
		if !ok {
			group = &slicerGroup{selection: *selection}
			groups[selection.ID] = group
			order = append(order, selection.ID)
		}
		if stripSlicer {
			group.items = append(group.items, withoutSlicerSelection(item, slicerType.ID))
		} else {
			group.items = append(group.items, item)
		}
	}

	// Alfabetik sıralama (case-insensitive) — .NET OrderBy(Name, OrdinalIgnoreCase).
	for i := 1; i < len(order); i++ {
		for j := i; j > 0 && strings.ToLower(groups[order[j-1]].selection.Name) > strings.ToLower(groups[order[j]].selection.Name); j-- {
			order[j-1], order[j] = order[j], order[j-1]
		}
	}

	productVariants := remainingTypes
	if !stripSlicer {
		productVariants = []VariantRef{slicerType}
	}

	overridesByValue := map[string]SplitOverride{}
	for _, candidate := range overrides {
		name := strings.ToLower(strings.TrimSpace(candidate.ValueName))
		if name == "" {
			continue
		}
		if _, exists := overridesByValue[name]; !exists {
			overridesByValue[name] = candidate
		}
	}

	usedModelCodes := map[string]struct{}{}
	plans := make([]CreatePlan, 0, len(order))
	for _, id := range order {
		group := groups[id]
		var groupOverride *SplitOverride
		if candidate, ok := overridesByValue[strings.ToLower(strings.TrimSpace(group.selection.Name))]; ok {
			groupOverride = &candidate
		}

		modelCodeResult := buildSplitModelCode(baseModelCode, group.selection, usedModelCodes, groupOverride)
		if modelCodeResult.IsFailure() {
			return sharedkernel.FailOf[[]CreatePlan](modelCodeResult.Err())
		}

		planName := strings.TrimSpace(baseName) + " - " + group.selection.Name
		if groupOverride != nil && groupOverride.Name != nil && strings.TrimSpace(*groupOverride.Name) != "" {
			planName = strings.TrimSpace(*groupOverride.Name)
		}
		var description *string
		if groupOverride != nil && groupOverride.Description != nil && strings.TrimSpace(*groupOverride.Description) != "" {
			trimmed := strings.TrimSpace(*groupOverride.Description)
			description = &trimmed
		}

		groupCode := baseModelCode
		slicerValue := group.selection.Name
		plans = append(plans, CreatePlan{
			ModelCode:   modelCodeResult.Value(),
			Name:        planName,
			Variants:    productVariants,
			Items:       group.items,
			GroupCode:   &groupCode,
			SlicerValue: &slicerValue,
			Description: description,
		})
	}
	return sharedkernel.OkOf(plans)
}

// buildSplitModelCode, bölünen ürünün model kodunu şu öncelikle üretir:
// pazaryerinden gelen gerçek kod → temel kod + değer slug'ı → temel kod + kısa id.
func buildSplitModelCode(baseModelCode string, selection VariantValue, used map[string]struct{}, override *SplitOverride) sharedkernel.ResultOf[string] {
	candidates := []string{}
	if override != nil && override.ModelCode != nil && strings.TrimSpace(*override.ModelCode) != "" {
		candidates = append(candidates, strings.TrimSpace(*override.ModelCode))
	}
	if slug := slugify(selection.Name); slug != "" {
		candidates = append(candidates, baseModelCode+"-"+slug)
	}
	shortID := strings.ReplaceAll(selection.ID.String(), "-", "")[:8]
	candidates = append(candidates, baseModelCode+"-"+shortID)

	for _, candidate := range candidates {
		lower := strings.ToLower(candidate)
		if _, exists := used[lower]; !exists {
			used[lower] = struct{}{}
			return sharedkernel.OkOf(candidate)
		}
	}
	return sharedkernel.FailOf[string](sharedkernel.NewValidationError(
		"Could not allocate a unique model code for slicer value '" + selection.Name + "'."))
}

// slugify, değeri küçük harfli harf/rakam dizisine indirger
// (.NET char.IsLetterOrDigit tam Unicode kabul eder; Go'da unicode paketi aynısını sağlar).
func slugify(value string) string {
	var b strings.Builder
	for _, ch := range strings.ToLower(strings.TrimSpace(value)) {
		if unicode.IsLetter(ch) || unicode.IsDigit(ch) {
			b.WriteRune(ch)
		}
	}
	return b.String()
}

// withoutSlicerSelection, kalemden slicer eksen seçimini çıkarır.
func withoutSlicerSelection(item ItemDraft, slicerTypeID uuid.UUID) ItemDraft {
	filtered := make([]VariantValue, 0, len(item.VariantValues))
	for _, selection := range item.VariantValues {
		if selection.Variant.ID != slicerTypeID {
			filtered = append(filtered, selection)
		}
	}
	item.VariantValues = filtered
	return item
}
