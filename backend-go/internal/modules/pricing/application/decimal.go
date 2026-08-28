// Package application, Pricing modülünün kullanım senaryolarını içerir
// (.NET Pricing.Application karşılığı). Tutarlar .NET decimal hassasiyetiyle
// bayt uyumlu taşınır: JSON'dan ham sayı olarak alınır, Postgres numeric'e
// dizgi olarak yazılır ve ::text ile ölçeği korunarak okunur — böylece 449.90
// hiçbir katmanda 449.9'a çökmez.
package application

import (
	"fmt"
	"math/big"
	"strings"
)

// Decimal, JSON sayısını ham (kayıpsız) biçimde taşıyan tutardır. Boş değer
// "yok" anlamına gelir (nullable alanlar *Decimal kullanır).
type Decimal string

// MarshalJSON, tutarı ham JSON sayısı olarak yazar.
func (d Decimal) MarshalJSON() ([]byte, error) {
	if d == "" {
		return []byte("null"), nil
	}
	if !d.IsValid() {
		return nil, fmt.Errorf("pricing: geçersiz ondalık değer: %q", string(d))
	}
	return []byte(d), nil
}

// UnmarshalJSON, JSON sayısını ham biçimde alır; null boş değere çözülür.
func (d *Decimal) UnmarshalJSON(data []byte) error {
	trimmed := strings.TrimSpace(string(data))
	if trimmed == "null" {
		*d = ""
		return nil
	}
	candidate := Decimal(trimmed)
	if !candidate.IsValid() {
		return fmt.Errorf("pricing: sayı bekleniyordu: %s", trimmed)
	}
	*d = candidate
	return nil
}

// rat, değeri kesin kesire çözer.
func (d Decimal) rat() (*big.Rat, bool) {
	r := new(big.Rat)
	_, ok := r.SetString(string(d))
	return r, ok
}

// IsValid, değerin geçerli bir ondalık sayı olup olmadığını döner.
func (d Decimal) IsValid() bool {
	if d == "" {
		return false
	}
	_, ok := d.rat()
	return ok
}

// IsNegative, değerin sıfırdan küçük olup olmadığını döner.
func (d Decimal) IsNegative() bool {
	r, ok := d.rat()
	return ok && r.Sign() < 0
}

// Equal, iki değerin sayısal eşitliğini döner ("449.9" == "449.90").
func (d Decimal) Equal(other Decimal) bool {
	a, aok := d.rat()
	b, bok := other.rat()
	return aok && bok && a.Cmp(b) == 0
}

// EqualPtr, opsiyonel değerlerin sayısal eşitliğini döner (ikisi de nil ise eşit).
func EqualPtr(a, b *Decimal) bool {
	if a == nil || b == nil {
		return a == nil && b == nil
	}
	return a.Equal(*b)
}
