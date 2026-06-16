// Package codegen builds product SKUs and EAN-13 barcodes. It is dependency-free
// (no DB) so it is fully unit-testable; uniqueness is enforced by the caller.
package codegen

import (
	"fmt"
	"regexp"
	"strconv"
	"strings"

	"github.com/kazimcavus/pimly/internal/shared/apperr"
)

// SKUParts are the segments of a product SKU: [group][color][size].
// product_sku is built from group + color; size lives on the variant axis.
type SKUParts struct {
	GroupCode string
	ColorCode string
	SizeCode  string
}

var skuCharset = regexp.MustCompile(`^[A-Za-z0-9._-]+$`)

// BuildSKU concatenates the non-empty SKU segments (uppercased) and validates
// the character set.
func BuildSKU(p SKUParts) (string, error) {
	var b strings.Builder
	for _, seg := range []string{p.GroupCode, p.ColorCode, p.SizeCode} {
		b.WriteString(strings.ToUpper(strings.TrimSpace(seg)))
	}
	sku := b.String()
	if sku == "" {
		return "", apperr.Validation("empty SKU")
	}
	if !skuCharset.MatchString(sku) {
		return "", apperr.Validation("invalid SKU %q", sku)
	}
	return sku, nil
}

// ValidateSKU checks an externally-supplied SKU's character set.
func ValidateSKU(sku string) error {
	if sku == "" || !skuCharset.MatchString(sku) {
		return apperr.Validation("invalid SKU %q", sku)
	}
	return nil
}

var digits12 = regexp.MustCompile(`^[0-9]{12}$`)
var digits13 = regexp.MustCompile(`^[0-9]{13}$`)

// internalPrefix is a GS1 restricted-distribution band (20–29) reserved for
// in-store / private use — guaranteed never assigned as a real company prefix.
// pimly-minted barcodes are therefore NOT GS1-registered.
const internalPrefix = "29"

// InternalEAN13Body builds the 12-digit body of an internal barcode:
// [29][tenantCode:4][serial:6]. Caller appends the check digit via GenerateEAN13.
func InternalEAN13Body(tenantCode int32, serial int64) string {
	return fmt.Sprintf("%s%04d%06d", internalPrefix, tenantCode%10000, serial%1000000)
}

// EAN13CheckDigit computes the mod-10 check digit for a 12-digit body.
func EAN13CheckDigit(body12 string) (int, error) {
	if !digits12.MatchString(body12) {
		return 0, apperr.Validation("EAN-13 body must be exactly 12 digits")
	}
	sum := 0
	for i := 0; i < 12; i++ {
		d := int(body12[i] - '0')
		if i%2 == 0 {
			sum += d // odd position (1-indexed): weight 1
		} else {
			sum += 3 * d // even position: weight 3
		}
	}
	return (10 - sum%10) % 10, nil
}

// GenerateEAN13 returns the full 13-digit barcode for a 12-digit body.
func GenerateEAN13(body12 string) (string, error) {
	cd, err := EAN13CheckDigit(body12)
	if err != nil {
		return "", err
	}
	return body12 + strconv.Itoa(cd), nil
}

// ValidateEAN13 verifies a 13-digit barcode's check digit.
func ValidateEAN13(code13 string) error {
	if !digits13.MatchString(code13) {
		return apperr.Validation("barcode must be exactly 13 digits")
	}
	cd, err := EAN13CheckDigit(code13[:12])
	if err != nil {
		return err
	}
	if int(code13[12]-'0') != cd {
		return apperr.Validation("invalid EAN-13 check digit")
	}
	return nil
}
