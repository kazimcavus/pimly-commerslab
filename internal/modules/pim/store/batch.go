package pimstore

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"strconv"
	"strings"

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
	Variants             []VariantInput  `json:"variants"`
}

type VariantInput struct {
	AxisValue        string          `json:"axis_value"`
	AxisValueEntryID *string         `json:"axis_value_entry_id"`
	Barcode          string          `json:"barcode"` // optional override
	Gtin             string          `json:"gtin"`
	Mpn              string          `json:"mpn"`
	Price            float64         `json:"price"`
	CompareAtPrice   *float64        `json:"compare_at_price"`
	Stock            int32           `json:"stock"`
	AttributeValues  json.RawMessage `json:"attribute_values"`
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

	for i, p := range in.Products {
		productStatus := defaultStatus(p.Status)
		if err := ValidateAttrs(ctx, q, categoryID, "product", p.AttributeValues, productStatus == "active"); err != nil {
			return nil, err
		}

		sku, err := resolveProductSKU(ctx, q, groupCode, p, i)
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
			barcode, err := resolveBarcode(ctx, q, t.BarcodeCode, v.Barcode)
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
			variant, err := q.CreateVariant(ctx, tenantdb.CreateVariantParams{
				ProductID:        product.ID,
				Barcode:          barcode,
				Gtin:             textOrNull(v.Gtin),
				Mpn:              textOrNull(v.Mpn),
				AxisValueEntryID: axisEntry,
				AxisValue:        textOrNull(v.AxisValue),
				Price:            price,
				CompareAtPrice:   comparePrice,
				Stock:            v.Stock,
				AttributeValues:  attrsOrEmpty(v.AttributeValues),
			})
			if err != nil {
				return nil, mapDBErr(err)
			}
			pr.Variants = append(pr.Variants, variant)
		}
		result.Products = append(result.Products, pr)
	}

	return result, nil
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

func resolveProductSKU(ctx context.Context, q *tenantdb.Queries, groupCode string, p ProductInput, idx int) (string, error) {
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
	colorCode := p.Code
	if colorCode == "" {
		colorCode = fmt.Sprintf("%02d", idx+1)
	}
	base, err := codegen.BuildSKU(codegen.SKUParts{GroupCode: groupCode, ColorCode: colorCode})
	if err != nil {
		return "", err
	}
	cand := base
	for n := 1; n <= 100; n++ {
		exists, err := q.ProductSkuExists(ctx, cand)
		if err != nil {
			return "", apperr.Internal(err)
		}
		if !exists {
			return cand, nil
		}
		cand = fmt.Sprintf("%s_%d", base, n+1)
	}
	return "", apperr.Conflict("could not allocate a unique product_sku for %q", base)
}

func resolveBarcode(ctx context.Context, q *tenantdb.Queries, tenantCode int32, override string) (string, error) {
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
	for n := 0; n < 100; n++ {
		serial, err := q.NextBarcodeSerial(ctx)
		if err != nil {
			return "", apperr.Internal(err)
		}
		barcode, err := codegen.GenerateEAN13(codegen.InternalEAN13Body(tenantCode, serial))
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
