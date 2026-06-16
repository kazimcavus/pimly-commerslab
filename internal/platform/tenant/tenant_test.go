package tenant

import (
	"strings"
	"testing"
)

func TestSlugify(t *testing.T) {
	cases := map[string]string{
		"Acme Tekstil":     "acme_tekstil",
		"Moda Butik":       "moda_butik",
		"Çağrı Şirketi":    "cagri_sirketi",
		"  Hello--World  ": "hello_world",
		"İstanbul Giyim":   "istanbul_giyim",
		"!!!":              "",
	}
	for in, want := range cases {
		if got := Slugify(in); got != want {
			t.Errorf("Slugify(%q) = %q, want %q", in, got, want)
		}
	}
}

func TestValidateSchemaName(t *testing.T) {
	valid := []string{"tenant_acme", "tenant_a1_b2", "tenant_x"}
	for _, s := range valid {
		if err := ValidateSchemaName(s); err != nil {
			t.Errorf("ValidateSchemaName(%q) unexpected error: %v", s, err)
		}
	}
	invalid := []string{"acme", "tenant_", "tenant_Acme", "tenant_a;drop", "public", "tenant_" + strings.Repeat("a", 49)}
	for _, s := range invalid {
		if err := ValidateSchemaName(s); err == nil {
			t.Errorf("ValidateSchemaName(%q) expected error, got nil", s)
		}
	}
}

func TestSchemaName(t *testing.T) {
	if got := SchemaName("acme"); got != "tenant_acme" {
		t.Errorf("SchemaName(acme) = %q, want tenant_acme", got)
	}
}
