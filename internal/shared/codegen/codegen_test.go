package codegen

import "testing"

func TestEAN13CheckDigit(t *testing.T) {
	cases := map[string]int{
		"400638133393": 1, // 4006381333931
		"978014300723": 4, // 9780143007234
		"012345678901": 2, // 0123456789012
	}
	for body, want := range cases {
		got, err := EAN13CheckDigit(body)
		if err != nil {
			t.Fatalf("EAN13CheckDigit(%s): %v", body, err)
		}
		if got != want {
			t.Errorf("EAN13CheckDigit(%s) = %d, want %d", body, got, want)
		}
	}
}

func TestGenerateAndValidateEAN13(t *testing.T) {
	full, err := GenerateEAN13("400638133393")
	if err != nil {
		t.Fatal(err)
	}
	if full != "4006381333931" {
		t.Fatalf("GenerateEAN13 = %s, want 4006381333931", full)
	}
	if err := ValidateEAN13(full); err != nil {
		t.Fatalf("ValidateEAN13(%s): %v", full, err)
	}
	if err := ValidateEAN13("4006381333930"); err == nil {
		t.Fatal("expected invalid check digit to fail")
	}
	if err := ValidateEAN13("12345"); err == nil {
		t.Fatal("expected short barcode to fail")
	}
}

func TestInternalEAN13Body(t *testing.T) {
	body := InternalEAN13Body(1, 42)
	if body != "290001000042" {
		t.Fatalf("InternalEAN13Body(1,42) = %s, want 290001000042", body)
	}
	full, err := GenerateEAN13(body)
	if err != nil {
		t.Fatal(err)
	}
	if err := ValidateEAN13(full); err != nil {
		t.Fatalf("generated internal barcode invalid: %v", err)
	}
}

func TestBuildSKU(t *testing.T) {
	sku, err := BuildSKU(SKUParts{GroupCode: "22Y265024", ColorCode: "R01"})
	if err != nil {
		t.Fatal(err)
	}
	if sku != "22Y265024R01" {
		t.Fatalf("BuildSKU = %s, want 22Y265024R01", sku)
	}
	if _, err := BuildSKU(SKUParts{}); err == nil {
		t.Fatal("expected empty SKU to fail")
	}
	if _, err := BuildSKU(SKUParts{GroupCode: "bad code!"}); err == nil {
		t.Fatal("expected invalid charset to fail")
	}
}
