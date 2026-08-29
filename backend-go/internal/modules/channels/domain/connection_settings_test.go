package domain

import "testing"

// TestExclusionRulesIsExcluded, kapsam dışı bırakma kurallarını Çağ Halı
// keşfinden çıkan gerçek SKU'larla doğrular: "Özel Ölçü" kayıtları (1.089
// varyant, barkodsuz, PIM'de karşılığı yok) mutabakata girmemeli; normal
// ürünler etkilenmemeli.
func TestExclusionRulesIsExcluded(t *testing.T) {
	rules := ExclusionRules{
		SkuPatterns: []string{"%-OZEL-%"},
		Statuses:    []string{"UNLISTED"},
	}

	cases := []struct {
		name   string
		sku    string
		status string
		want   bool
	}{
		{"özel ölçü SKU'su desene uyar", "26AKR0009R05-OZEL-100x134-SACAKLI", "ACTIVE", true},
		{"ikinci özel ölçü kaydı", "25CSM0008R06-OZEL-80x50-SACAKLI", "ACTIVE", true},
		{"durum eşleşmesi tek başına yeter", "26JUT0025R06-120x180", "UNLISTED", true},
		{"durum karşılaştırması harf duyarsız", "26JUT0025R06-120x180", "unlisted", true},
		{"desen karşılaştırması harf duyarsız", "26akr0009r05-ozel-100x134-sacakli", "ACTIVE", true},
		{"normal ürün kapsam dışı değil", "25BHR0001R15-160x230", "ACTIVE", false},
		{"ölçüsüz normal ürün kapsam dışı değil", "25HLT0001R08", "ACTIVE", false},
		{"taslak ürün kapsam dışı değil", "25SIS0008R02-80x80", "DRAFT", false},
		{"OZEL kelimesi geçse de ayraçsızsa eşleşmez", "26OZEL0001R05-80x150", "ACTIVE", false},
	}

	for _, tc := range cases {
		t.Run(tc.name, func(t *testing.T) {
			if got := rules.IsExcluded(tc.sku, tc.status); got != tc.want {
				t.Fatalf("IsExcluded(%q, %q) = %v; beklenen %v", tc.sku, tc.status, got, tc.want)
			}
		})
	}
}

// TestExclusionRulesEmpty, kural tanımlanmamışsa hiçbir kaydın elenmediğini
// doğrular — varsayılan davranış "her şeye bak" olmalı.
func TestExclusionRulesEmpty(t *testing.T) {
	var rules ExclusionRules
	if rules.IsExcluded("26AKR0009R05-OZEL-100x134-SACAKLI", "UNLISTED") {
		t.Fatal("kural yokken hiçbir kayıt kapsam dışı sayılmamalı")
	}
	// Boş desen de eşleşmemeli; aksi halde tek bir boş satır tüm katalogu eler.
	blank := ExclusionRules{SkuPatterns: []string{"", "   "}}
	if blank.IsExcluded("herhangi-bir-sku", "ACTIVE") {
		t.Fatal("boş desen hiçbir şeyi elememeli")
	}
}

// TestLikeMatch, SQL LIKE joker karakterlerinin beklendiği gibi çalıştığını
// doğrular ('%' herhangi bir dizi, '_' tek karakter).
func TestLikeMatch(t *testing.T) {
	cases := []struct {
		value, pattern string
		want           bool
	}{
		{"abc", "abc", true},
		{"abc", "a%", true},
		{"abc", "%c", true},
		{"abc", "%b%", true},
		{"abc", "a_c", true},
		{"abc", "a_", false},
		{"abc", "%d%", false},
		{"abc", "", false},
		{"", "%", true},
		{"a-OZEL-b", "%-OZEL-%", true},
		{"OZEL-b", "%-OZEL-%", false},
		// Ardışık '%' ve sondaki '%' yığını geri izlemeyi bozmamalı.
		{"aXbXc", "a%b%c%", true},
		{"aXbXc", "a%%c", true},
		{"abcabc", "%abc", true},
	}
	for _, tc := range cases {
		if got := matchesLikePattern(tc.value, tc.pattern); got != tc.want {
			t.Errorf("matchesLikePattern(%q, %q) = %v; beklenen %v", tc.value, tc.pattern, got, tc.want)
		}
	}
}
