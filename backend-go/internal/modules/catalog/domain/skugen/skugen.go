// Package skugen, SKU/model kodu üreticisinin yapılandırmasını ve kod montaj
// mantığını içerir (.NET Catalog.Domain.SkuGenerator karşılığı). Yapılandırma
// tenant başına tek satırdır; segment şablonundan ürün kodu ve varyant SKU'su üretilir.
package skugen

import (
	"fmt"
	"strings"
	"time"

	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// SingletonID, tenant başına tek yapılandırma satırının sabit kimliğidir.
const SingletonID = 1

// BasePlaceholder, generator yolunda splitter'a verilen geçici temel koddur;
// nihai kod üretildiğinde yerine konur.
const BasePlaceholder = "__SKU__"

// Segment tipleri.
const (
	SegmentFixed   = "fixed"
	SegmentManual  = "manual"
	SegmentCounter = "counter"
	SegmentYear    = "year"
	SegmentColor   = "color"
	SegmentSize    = "size"
)

// Segment, SKU şablonundaki tek bir segment tanımıdır. JSON etiketleri hem
// veritabanı jsonb biçimi hem API kablo biçimidir; .NET camelCase serileştirir
// ve türetilmiş isCounterSegment/isVariantSegment alanlarını da yazar.
type Segment struct {
	Type   string  `json:"type"`
	Label  *string `json:"label"`
	Value  *string `json:"value"`
	Start  *int    `json:"start"`
	Width  *int    `json:"width"`
	Digits *int    `json:"digits"`
	Source *string `json:"source"`
}

// IsVariantSegment, segmentin color/size varyant token'ı üretip üretmediğini döner.
func (s Segment) IsVariantSegment() bool {
	return strings.EqualFold(s.Type, SegmentColor) || strings.EqualFold(s.Type, SegmentSize)
}

// IsCounterSegment, segmentin artan sayaç token'ı üretip üretmediğini döner.
func (s Segment) IsCounterSegment() bool { return strings.EqualFold(s.Type, SegmentCounter) }

// Config, SKU oluşturucu yapılandırmasıdır — tenant başına tek satır.
type Config struct {
	// Enabled, generator'ın açık olup olmadığını belirtir.
	Enabled bool

	// Segments, sıralı segment şablonudur.
	Segments []Segment

	// CounterNextValue, bir sonraki counter token değeridir.
	CounterNextValue int64
}

// NewInitialConfig, varsayılan ayarlarla kapalı başlangıç yapılandırması oluşturur.
func NewInitialConfig() *Config {
	return &Config{Enabled: false, Segments: []Segment{}, CounterNextValue: 1}
}

// DefaultCounterStart, şablondaki ilk counter segmentinin başlangıç değerini
// döner; yoksa 1.
func DefaultCounterStart(segments []Segment) int64 {
	for _, segment := range segments {
		if segment.IsCounterSegment() && segment.Start != nil && *segment.Start > 0 {
			return int64(*segment.Start)
		}
	}
	return 1
}

// CounterSegmentCount, şablondaki counter segment sayısını döner.
func (c *Config) CounterSegmentCount() int {
	count := 0
	for _, segment := range c.Segments {
		if segment.IsCounterSegment() {
			count++
		}
	}
	return count
}

// UpdateSettings, generator durumunu, segment şablonunu ve opsiyonel counter
// değerini günceller; counter geriye alınamaz.
func (c *Config) UpdateSettings(enabled bool, segments []Segment, counterNextValue *int64) sharedkernel.Result {
	if len(segments) == 0 && enabled {
		return sharedkernel.Fail(sharedkernel.NewValidationError(
			"At least one segment is required when the SKU generator is enabled."))
	}
	c.Enabled = enabled
	c.Segments = segments

	if counterNextValue != nil {
		if *counterNextValue < 1 {
			return sharedkernel.Fail(sharedkernel.NewValidationError("Counter next value must be at least 1."))
		}
		if *counterNextValue < c.CounterNextValue {
			return sharedkernel.Fail(sharedkernel.NewConflictError(fmt.Sprintf(
				"Counter next value must be at least the current value (%d).", c.CounterNextValue)))
		}
		c.CounterNextValue = *counterNextValue
	} else if c.CounterNextValue < 1 {
		c.CounterNextValue = DefaultCounterStart(segments)
	}
	return sharedkernel.Ok()
}

// EnsureCounterInitialized, counter geçersizse şablondan varsayılan başlangıcı atar.
func (c *Config) EnsureCounterInitialized() {
	if c.CounterNextValue < 1 {
		c.CounterNextValue = DefaultCounterStart(c.Segments)
	}
}

// VariantSelection, SKU üretiminde kullanılan varyant değeri anlık görüntüsüdür.
type VariantSelection struct {
	// SelectionStyle, değerin ait olduğu eksenin stilidir ("list"/"color").
	SelectionStyle string

	// Name, değerin görünen adıdır.
	Name string

	// Key, değerin anahtarıdır; nil olabilir.
	Key *string
}

// AssembleProductCode, ürün seviyesi kodu üretir (color/size segmentleri hariç)
// ve güncellenmiş counter değerini döner.
func AssembleProductCode(segments []Segment, codeInputs []string, counterValue int64, utcNow time.Time) sharedkernel.ResultOf[struct {
	Code        string
	NextCounter int64
}] {
	type result = struct {
		Code        string
		NextCounter int64
	}
	year := utcNow.Year()
	var b strings.Builder

	for index, segment := range segments {
		if segment.IsVariantSegment() {
			continue
		}
		switch strings.ToLower(segment.Type) {
		case SegmentFixed:
			value := ""
			if segment.Value != nil {
				value = *segment.Value
			}
			b.WriteString(strings.ToUpper(strings.TrimSpace(value)))
		case SegmentManual:
			var value string
			if index < len(codeInputs) {
				value = codeInputs[index]
			}
			if strings.TrimSpace(value) == "" {
				label := "manual segment"
				if segment.Label != nil && strings.TrimSpace(*segment.Label) != "" {
					label = *segment.Label
				}
				return sharedkernel.FailOf[result](sharedkernel.NewValidationError(
					fmt.Sprintf("Manual segment '%s' is required.", label)))
			}
			b.WriteString(strings.ToUpper(strings.TrimSpace(value)))
		case SegmentCounter:
			width := 4
			if segment.Width != nil && *segment.Width > 0 {
				width = *segment.Width
			}
			b.WriteString(fmt.Sprintf("%0*d", width, counterValue))
			counterValue++
		case SegmentYear:
			if segment.Digits != nil && *segment.Digits == 4 {
				b.WriteString(fmt.Sprintf("%04d", year))
			} else {
				b.WriteString(fmt.Sprintf("%02d", year%100))
			}
		}
	}
	return sharedkernel.OkOf(result{Code: b.String(), NextCounter: counterValue})
}

// AssembleVariantSku, varyant SKU'sunu üretir: ürün kodu + color/size tokenları.
func AssembleVariantSku(productCode string, segments []Segment, selections []VariantSelection) string {
	var b strings.Builder
	b.WriteString(productCode)
	for _, segment := range segments {
		if strings.EqualFold(segment.Type, SegmentColor) {
			for _, selection := range selections {
				if selection.SelectionStyle == "color" {
					b.WriteString(variantToken(selection, segment.Source))
					break
				}
			}
		} else if strings.EqualFold(segment.Type, SegmentSize) {
			for _, selection := range selections {
				if selection.SelectionStyle != "color" {
					b.WriteString(variantToken(selection, segment.Source))
				}
			}
		}
	}
	return b.String()
}

// variantToken, varyant değerinden token üretir: source "name" ise ad, aksi
// halde anahtar (yoksa ad) büyük harfle kullanılır.
func variantToken(selection VariantSelection, source *string) string {
	useName := source != nil && strings.EqualFold(*source, "name")
	raw := selection.Name
	if !useName && selection.Key != nil {
		raw = *selection.Key
	}
	return strings.ToUpper(strings.TrimSpace(raw))
}

// ValidateManualInputs, manual segment girdilerinin eksiksizliğini doğrular.
func ValidateManualInputs(segments []Segment, codeInputs []string) sharedkernel.Result {
	for index, segment := range segments {
		if !strings.EqualFold(segment.Type, SegmentManual) {
			continue
		}
		var value string
		if index < len(codeInputs) {
			value = codeInputs[index]
		}
		if strings.TrimSpace(value) == "" {
			label := "manual segment"
			if segment.Label != nil && strings.TrimSpace(*segment.Label) != "" {
				label = *segment.Label
			}
			return sharedkernel.Fail(sharedkernel.NewValidationError(
				fmt.Sprintf("Manual segment '%s' is required.", label)))
		}
	}
	return sharedkernel.Ok()
}

// ValidateVariantCodes, key kaynaklı varyant segmentleri için değer
// anahtarlarının tanımlı olduğunu doğrular ("code" eski eşanlamlıdır).
func ValidateVariantCodes(segments []Segment, selections []VariantSelection) sharedkernel.Result {
	usesKeySource := func(source *string) bool {
		return source != nil && (strings.EqualFold(*source, "key") || strings.EqualFold(*source, "code"))
	}
	for _, segment := range segments {
		if !segment.IsVariantSegment() || !usesKeySource(segment.Source) {
			continue
		}
		isColor := strings.EqualFold(segment.Type, SegmentColor)
		for _, selection := range selections {
			matches := (isColor && selection.SelectionStyle == "color") ||
				(!isColor && selection.SelectionStyle != "color")
			if matches && (selection.Key == nil || strings.TrimSpace(*selection.Key) == "") {
				return sharedkernel.Fail(sharedkernel.NewValidationError(fmt.Sprintf(
					"Variant value '%s' requires a key for SKU segment '%s'.", selection.Name, segment.Type)))
			}
		}
	}
	return sharedkernel.Ok()
}
