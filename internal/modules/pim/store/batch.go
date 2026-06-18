package pimstore

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"strconv"
	"strings"
	"time"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgconn"
	"github.com/jackc/pgx/v5/pgtype"

	"github.com/kazimcavus/pimly/internal/platform/db/tenantdb"
	"github.com/kazimcavus/pimly/internal/platform/tenant"
	"github.com/kazimcavus/pimly/internal/shared/apperr"
	"github.com/kazimcavus/pimly/internal/shared/codegen"
)

// BatchInput is the single write-path payload: a group with its products and
// each product's (ragged) variant set.
type BatchInput struct {
	Group    GroupInput     `json:"group"`
	Products []ProductInput `json:"products"`
}

type GroupInput struct {
	GroupCode       string          `json:"group_code"`
	CategoryID      *string         `json:"category_id"`
	Title           string          `json:"title"`
	Status          string          `json:"status"`
	AttributeValues json.RawMessage `json:"attribute_values"`
}

type ProductInput struct {
	Code                 string          `json:"code"`        // color/product segment for the SKU
	ProductSku           string          `json:"product_sku"` // optional override
	GroupingValueEntryID *string         `json:"grouping_value_entry_id"`
	Title                string          `json:"title"`
	Status               string          `json:"status"`
	AttributeValues      json.RawMessage `json:"attribute_values"`
	VariantTypes         json.RawMessage `json:"variant_types"` // ordered chosen types (flat model)
	CodeInputs           []string        `json:"code_inputs"`   // SKU template: season/manual segment values (by segment index)
	Variants             []VariantInput  `json:"variants"`
}

type VariantInput struct {
	AxisValue        string          `json:"axis_value"`
	AxisValueEntryID *string         `json:"axis_value_entry_id"`
	Sku              string          `json:"sku"` // optional override; auto-generated otherwise
	Barcode          string          `json:"barcode"` // optional override
	Gtin             string          `json:"gtin"`
	Mpn              string          `json:"mpn"`
	Price            float64         `json:"price"`
	CompareAtPrice   *float64        `json:"compare_at_price"`
	Stock            int32           `json:"stock"`
	AttributeValues  json.RawMessage `json:"attribute_values"`
	Options          json.RawMessage `json:"options"` // [{type_id,type_name,value_id,value_label,color,image_url}]
}

// BatchResult is the created tree.
type BatchResult struct {
	Group    tenantdb.Group  `json:"group"`
	Products []ProductResult `json:"products"`
}

type ProductResult struct {
	tenantdb.Product
	Variants []tenantdb.Variant `json:"variants"`
}

// CreateBatch creates the whole group→product→variant tree in one transaction,
// generating SKUs and barcodes where not supplied and enforcing uniqueness and
// attribute validation.
func CreateBatch(ctx context.Context, tx pgx.Tx, t tenant.Tenant, in BatchInput) (*BatchResult, error) {
	q := tenantdb.New(tx)

	categoryID, err := parsePtrUUID(in.Group.CategoryID, "category_id")
	if err != nil {
		return nil, err
	}

	groupStatus := defaultStatus(in.Group.Status)
	groupCode, err := resolveGroupCode(ctx, q, in.Group.GroupCode)
	if err != nil {
		return nil, err
	}
	if err := ValidateAttrs(ctx, q, categoryID, "group", in.Group.AttributeValues, groupStatus == "active"); err != nil {
		return nil, err
	}

	group, err := q.CreateGroup(ctx, tenantdb.CreateGroupParams{
		GroupCode:       groupCode,
		CategoryID:      categoryID,
		Title:           in.Group.Title,
		Status:          groupStatus,
		AttributeValues: attrsOrEmpty(in.Group.AttributeValues),
	})
	if err != nil {
		return nil, mapDBErr(err)
	}

	result := &BatchResult{Group: group}

	// Barcode generator config (optional). When enabled, barcodes count up from
	// a configured start; otherwise the internal "29"+tenant serial is used.
	bcfg, bcEnabled := loadBarcodeCfg(ctx, q)
	bcNext := bcfg.Next
	if bcNext <= 0 {
		bcNext = barcodeStartBody(bcfg.Start)
	}
	bcStartNext := bcNext

	// SKU generator config (optional). When enabled, product/variant SKUs are
	// assembled from the configured segment template + per-product inputs.
	scfg, scEnabled := loadSkuCfg(ctx, q)
	scNext := scfg.Next
	if scNext <= 0 {
		scNext = skuCounterStart(scfg)
	}
	scStartNext := scNext

	for i, p := range in.Products {
		productStatus := defaultStatus(p.Status)
		if err := ValidateAttrs(ctx, q, categoryID, "product", p.AttributeValues, productStatus == "active"); err != nil {
			return nil, err
		}

		sku, err := resolveProductSKU(ctx, q, groupCode, p, i, scEnabled, scfg, &scNext)
		if err != nil {
			return nil, err
		}
		groupingEntry, err := parsePtrUUID(p.GroupingValueEntryID, "grouping_value_entry_id")
		if err != nil {
			return nil, err
		}

		product, err := q.CreateProduct(ctx, tenantdb.CreateProductParams{
			GroupID:              group.ID,
			ProductSku:           sku,
			GroupingValueEntryID: groupingEntry,
			Title:                p.Title,
			AttributeValues:      attrsOrEmpty(p.AttributeValues),
			Status:               productStatus,
			VariantTypes:         jsonArrayOrEmpty(p.VariantTypes),
		})
		if err != nil {
			return nil, mapDBErr(err)
		}

		pr := ProductResult{Product: product}
		for _, v := range p.Variants {
			// variants share the product's status for required-attribute enforcement
			if err := ValidateAttrs(ctx, q, categoryID, "variant", v.AttributeValues, productStatus == "active"); err != nil {
				return nil, err
			}
			barcode, err := resolveBarcodeCfg(ctx, q, v.Barcode, bcEnabled, &bcNext)
			if err != nil {
				return nil, err
			}
			price, err := toNumeric(v.Price)
			if err != nil {
				return nil, err
			}
			comparePrice, err := toNullableNumeric(v.CompareAtPrice)
			if err != nil {
				return nil, err
			}
			axisEntry, err := parsePtrUUID(v.AxisValueEntryID, "axis_value_entry_id")
			if err != nil {
				return nil, err
			}
			var vopts []variantOpt
			if len(v.Options) > 0 {
				_ = json.Unmarshal(v.Options, &vopts)
			}
			variantSKU, err := resolveVariantSKU(ctx, q, sku, v.Sku, scEnabled, scfg, vopts)
			if err != nil {
				return nil, err
			}
			variant, err := q.CreateVariant(ctx, tenantdb.CreateVariantParams{
				ProductID:        product.ID,
				Sku:              variantSKU,
				Barcode:          barcode,
				Gtin:             textOrNull(v.Gtin),
				Mpn:              textOrNull(v.Mpn),
				AxisValueEntryID: axisEntry,
				AxisValue:        textOrNull(v.AxisValue),
				Price:            price,
				CompareAtPrice:   comparePrice,
				Stock:            v.Stock,
				AttributeValues:  attrsOrEmpty(v.AttributeValues),
				Options:          jsonArrayOrEmpty(v.Options),
			})
			if err != nil {
				return nil, mapDBErr(err)
			}
			pr.Variants = append(pr.Variants, variant)
		}
		result.Products = append(result.Products, pr)
	}

	// Persist the advanced barcode counter so the next batch continues counting.
	if bcEnabled && bcNext != bcStartNext {
		raw, _ := json.Marshal(barcodeCfg{Enabled: true, Start: bcfg.Start, Next: bcNext})
		if _, err := q.UpsertSetting(ctx, tenantdb.UpsertSettingParams{Key: "barcode", Value: raw}); err != nil {
			return nil, apperr.Internal(err)
		}
	}
	// Persist the advanced SKU counter likewise.
	if scEnabled && scNext != scStartNext {
		scfg.Next = scNext
		raw, _ := json.Marshal(scfg)
		if _, err := q.UpsertSetting(ctx, tenantdb.UpsertSettingParams{Key: "sku", Value: raw}); err != nil {
			return nil, apperr.Internal(err)
		}
	}

	return result, nil
}

// --- SKU generator ---

type skuCfg struct {
	Enabled  bool         `json:"enabled"`
	Segments []skuSegment `json:"segments"`
	Next     int64        `json:"next"`
}

type skuSegment struct {
	Type   string `json:"type"`   // fixed | manual | counter | year | color | size (legacy: season)
	Value  string `json:"value"`  // fixed
	Label  string `json:"label"`  // user-defined title (manual/any)
	Start  int64  `json:"start"`  // counter
	Width  int    `json:"width"`  // counter
	Digits int    `json:"digits"` // year (2 or 4)
	Source string `json:"source"` // color/size: "code" | "name"
}

func loadSkuCfg(ctx context.Context, q *tenantdb.Queries) (skuCfg, bool) {
	s, err := q.GetSetting(ctx, "sku")
	if err != nil {
		return skuCfg{}, false
	}
	var c skuCfg
	if err := json.Unmarshal(s.Value, &c); err != nil {
		return skuCfg{}, false
	}
	return c, c.Enabled && len(c.Segments) > 0
}

func skuCounterStart(c skuCfg) int64 {
	for _, s := range c.Segments {
		if s.Type == "counter" {
			if s.Start > 0 {
				return s.Start
			}
			return 1
		}
	}
	return 0
}

func skuIsVariantSeg(t string) bool { return t == "color" || t == "size" }

// assembleProductSKU builds the product-level code from the non-variant segments
// using per-product inputs (season/manual, by segment index) and the counter.
func assembleProductSKU(c skuCfg, inputs []string, next *int64) (string, error) {
	var b strings.Builder
	for i, seg := range c.Segments {
		switch seg.Type {
		case "color", "size":
			continue
		case "fixed":
			b.WriteString(strings.ToUpper(strings.TrimSpace(seg.Value)))
		case "season", "manual":
			v := ""
			if i < len(inputs) {
				v = strings.TrimSpace(inputs[i])
			}
			if v == "" {
				label := seg.Label
				if label == "" {
					label = seg.Type
				}
				return "", apperr.Validation("ürün kodu için %q gerekli", label)
			}
			b.WriteString(strings.ToUpper(v))
		case "counter":
			w := seg.Width
			if w <= 0 {
				w = 4
			}
			b.WriteString(fmt.Sprintf("%0*d", w, *next))
			*next++
		case "year":
			y := time.Now().Year()
			if seg.Digits == 4 {
				b.WriteString(fmt.Sprintf("%04d", y))
			} else {
				b.WriteString(fmt.Sprintf("%02d", y%100))
			}
		}
	}
	return codegen.BuildSKU(codegen.SKUParts{GroupCode: b.String()})
}

type variantOpt struct {
	ValueLabel string `json:"value_label"`
	Color      string `json:"color"`
	ImageURL   string `json:"image_url"`
	Code       string `json:"code"`
}

// optToken returns the SKU token for an option value: its name, or its code
// (falling back to name when no code), per the segment's source setting.
func optToken(o variantOpt, source string) string {
	if source == "name" {
		return strings.ToUpper(o.ValueLabel)
	}
	if o.Code != "" {
		return strings.ToUpper(o.Code)
	}
	return strings.ToUpper(o.ValueLabel)
}

// assembleVariantSKU appends the variant (color/size) segment tokens to the
// product SKU, pulling codes/names from the variant's option combination.
func assembleVariantSKU(productSKU string, c skuCfg, opts []variantOpt) string {
	var b strings.Builder
	b.WriteString(productSKU)
	for _, seg := range c.Segments {
		switch seg.Type {
		case "color":
			for _, o := range opts {
				if o.Color != "" || o.ImageURL != "" {
					b.WriteString(optToken(o, seg.Source))
					break
				}
			}
		case "size":
			for _, o := range opts {
				if o.Color == "" && o.ImageURL == "" {
					b.WriteString(optToken(o, seg.Source))
				}
			}
		}
	}
	return b.String()
}

// barcodeCfg is the persisted barcode generator setting.
type barcodeCfg struct {
	Enabled bool  `json:"enabled"`
	Start   int64 `json:"start"`
	Next    int64 `json:"next"`
}

// barcodeStartBody turns a configured prefix (e.g. 8440491) into the 12-digit
// EAN-13 body start by right-padding with a serial field (844049100000). The
// counter then increments by 1; when the serial field overflows the prefix
// naturally rolls (…99999 → next prefix 8440492·00000).
func barcodeStartBody(start int64) int64 {
	if start <= 0 {
		return 0
	}
	s := strconv.FormatInt(start, 10)
	w := 12 - len(s)
	if w <= 0 {
		return start
	}
	m := int64(1)
	for i := 0; i < w; i++ {
		m *= 10
	}
	return start * m
}

func loadBarcodeCfg(ctx context.Context, q *tenantdb.Queries) (barcodeCfg, bool) {
	s, err := q.GetSetting(ctx, "barcode")
	if err != nil {
		return barcodeCfg{}, false
	}
	var c barcodeCfg
	if err := json.Unmarshal(s.Value, &c); err != nil {
		return barcodeCfg{}, false
	}
	return c, c.Enabled && c.Start > 0
}

// resolveBarcodeCfg validates an explicit barcode, or generates from the
// configured counter when enabled. With no override and no generator it errors —
// the user must supply a barcode (no silent auto-generation).
func resolveBarcodeCfg(ctx context.Context, q *tenantdb.Queries, override string, enabled bool, next *int64) (string, error) {
	if override != "" {
		if err := codegen.ValidateEAN13(override); err != nil {
			return "", err
		}
		exists, err := q.VariantBarcodeExists(ctx, override)
		if err != nil {
			return "", apperr.Internal(err)
		}
		if exists {
			return "", apperr.Conflict("barcode %q already exists", override)
		}
		return override, nil
	}
	if !enabled {
		return "", apperr.Validation("barkod gerekli — elle girin ya da Ayarlar'dan barkod üreticisini açın")
	}
	for n := 0; n < 100000; n++ {
		body := fmt.Sprintf("%012d", *next)
		*next++
		barcode, err := codegen.GenerateEAN13(body)
		if err != nil {
			return "", apperr.Internal(err)
		}
		exists, err := q.VariantBarcodeExists(ctx, barcode)
		if err != nil {
			return "", apperr.Internal(err)
		}
		if !exists {
			return barcode, nil
		}
	}
	return "", apperr.Conflict("could not allocate a unique barcode")
}

func resolveGroupCode(ctx context.Context, q *tenantdb.Queries, code string) (string, error) {
	if code != "" {
		exists, err := q.GroupCodeExists(ctx, code)
		if err != nil {
			return "", apperr.Internal(err)
		}
		if exists {
			return "", apperr.Conflict("group_code %q already exists", code)
		}
		return code, nil
	}
	// Generate a unique fallback group code.
	for n := 0; n < 50; n++ {
		cand := "G" + strings.ToUpper(uuid.NewString()[:8])
		exists, err := q.GroupCodeExists(ctx, cand)
		if err != nil {
			return "", apperr.Internal(err)
		}
		if !exists {
			return cand, nil
		}
	}
	return "", apperr.Conflict("could not allocate a unique group code")
}

func resolveProductSKU(ctx context.Context, q *tenantdb.Queries, groupCode string, p ProductInput, idx int, scEnabled bool, scfg skuCfg, scNext *int64) (string, error) {
	if p.ProductSku != "" {
		if err := codegen.ValidateSKU(p.ProductSku); err != nil {
			return "", err
		}
		exists, err := q.ProductSkuExists(ctx, p.ProductSku)
		if err != nil {
			return "", apperr.Internal(err)
		}
		if exists {
			return "", apperr.Conflict("product_sku %q already exists", p.ProductSku)
		}
		return p.ProductSku, nil
	}
	// SKU generator: assemble from the configured template. A duplicate product
	// code is rejected (no _N suffix) — only new variants may share a product.
	if scEnabled {
		sku, err := assembleProductSKU(scfg, p.CodeInputs, scNext)
		if err != nil {
			return "", err
		}
		exists, err := q.ProductSkuExists(ctx, sku)
		if err != nil {
			return "", apperr.Internal(err)
		}
		if exists {
			return "", apperr.Conflict("bu ürün koduyla kayıt mevcut: %q (yeni varyantı ürün içinden ekleyin)", sku)
		}
		return sku, nil
	}
	// No override and no generator: the user must supply the code (no silent auto).
	return "", apperr.Validation("ürün kodu gerekli — elle girin ya da Ayarlar'dan ürün kodu üreticisini açın")
}

// resolveVariantSKU returns the per-variant SKU. An explicit override is
// validated; the generator assembles from the template; otherwise the variant
// SKU is left empty (NULL) — no silent auto-generation.
func resolveVariantSKU(ctx context.Context, q *tenantdb.Queries, productSKU string, override string, scEnabled bool, scfg skuCfg, opts []variantOpt) (pgtype.Text, error) {
	var sku string
	switch {
	case override != "":
		if err := codegen.ValidateSKU(override); err != nil {
			return pgtype.Text{}, err
		}
		exists, err := q.VariantSkuExists(ctx, pgtype.Text{String: override, Valid: true})
		if err != nil {
			return pgtype.Text{}, apperr.Internal(err)
		}
		if exists {
			return pgtype.Text{}, apperr.Conflict("variant sku %q already exists", override)
		}
		sku = override
	case scEnabled && len(opts) > 0:
		sku = assembleVariantSKU(productSKU, scfg, opts)
		exists, err := q.VariantSkuExists(ctx, pgtype.Text{String: sku, Valid: true})
		if err != nil {
			return pgtype.Text{}, apperr.Internal(err)
		}
		if exists {
			return pgtype.Text{}, apperr.Conflict("varyant sku zaten mevcut: %q", sku)
		}
	default:
		return pgtype.Text{}, nil // NULL — optional
	}
	return pgtype.Text{String: sku, Valid: true}, nil
}

// jsonArrayOrEmpty normalizes a JSONB array payload to a non-null value.
func jsonArrayOrEmpty(raw json.RawMessage) json.RawMessage {
	if len(raw) == 0 || string(raw) == "null" {
		return json.RawMessage("[]")
	}
	return raw
}

func defaultStatus(s string) string {
	if s == "" {
		return "draft"
	}
	return s
}

func parsePtrUUID(s *string, field string) (*uuid.UUID, error) {
	if s == nil || *s == "" {
		return nil, nil
	}
	id, err := uuid.Parse(*s)
	if err != nil {
		return nil, apperr.Validation("invalid %s", field)
	}
	return &id, nil
}

func textOrNull(s string) pgtype.Text {
	if s == "" {
		return pgtype.Text{}
	}
	return pgtype.Text{String: s, Valid: true}
}

func toNumeric(f float64) (pgtype.Numeric, error) {
	var n pgtype.Numeric
	if err := n.Scan(strconv.FormatFloat(f, 'f', -1, 64)); err != nil {
		return n, apperr.Validation("invalid numeric value %v", f)
	}
	return n, nil
}

func toNullableNumeric(f *float64) (pgtype.Numeric, error) {
	if f == nil {
		return pgtype.Numeric{}, nil
	}
	return toNumeric(*f)
}

// mapDBErr maps unique/fk/check violations to typed errors; passes existing
// apperr through and falls back to internal.
func mapDBErr(err error) error {
	var ae *apperr.Error
	if errors.As(err, &ae) {
		return err
	}
	var pgErr *pgconn.PgError
	if errors.As(err, &pgErr) {
		switch pgErr.Code {
		case "23505":
			return apperr.Conflict("already exists")
		case "23503":
			return apperr.Validation("referenced entity does not exist")
		case "23514":
			return apperr.Validation("value violates a constraint")
		}
	}
	return apperr.Internal(err)
}
