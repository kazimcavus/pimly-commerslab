package parity

import (
	"fmt"
	"net/http"
	"testing"
	"time"
)

// identityMasksLoginOK, başarılı giriş/kayıt yanıtının volatil alanlarıdır.
var identityMasksLoginOK = map[string]string{
	"token":      MaskJWT,
	"expires_at": MaskDateTime,
	"user.id":    MaskUUID,
	"tenant.id":  MaskUUID,
}

// problemMasks, ProblemDetails gövdesinin volatil alanıdır: .NET trace_id'yi
// her zaman doldurur, Go'da izleme kapalıyken boş dizgidir — her ikisi geçerli.
var problemMasks = map[string]string{"trace_id": MaskAnyString}

// mergeMasks, birden çok mask kümesini birleştirir.
func mergeMasks(sets ...map[string]string) map[string]string {
	out := map[string]string{}
	for _, set := range sets {
		for k, v := range set {
			out[k] = v
		}
	}
	return out
}

// TestIdentityParity, Identity modülünün kablo formatı paritesini doğrular.
// PARITY_BASE_URL tanımlı değilse atlanır; PARITY_MODE=capture golden üretir.
func TestIdentityParity(t *testing.T) {
	r := NewRunnerFromEnv("goldens")
	if r == nil {
		t.Skip("PARITY_BASE_URL tanımlı değil; parite testi atlandı")
	}
	if err := r.Login("owner@acme.test", "demo1234"); err != nil {
		t.Fatalf("koşucu girişi: %v", err)
	}

	// Kayıt senaryosu her koşuda taze e-posta ister; e-posta yanıtta yankılanır,
	// bu yüzden maskeyle karşılaştırılır.
	freshEmail := fmt.Sprintf("parity-%d@example.com", time.Now().UnixNano())

	cases := []Case{
		{
			Name:   "identity/login_ok",
			Method: http.MethodPost,
			Path:   "/api/v1/identity/login",
			Body:   map[string]string{"email": "owner@acme.test", "password": "demo1234"},
			Masks:  identityMasksLoginOK,
		},
		{
			Name:   "identity/login_wrong_password",
			Method: http.MethodPost,
			Path:   "/api/v1/identity/login",
			Body:   map[string]string{"email": "owner@acme.test", "password": "wrong-password"},
			Masks:  problemMasks,
		},
		{
			Name:   "identity/login_unknown_email",
			Method: http.MethodPost,
			Path:   "/api/v1/identity/login",
			Body:   map[string]string{"email": "nobody@example.com", "password": "whatever123"},
			Masks:  problemMasks,
		},
		{
			Name:   "identity/login_validation_empty",
			Method: http.MethodPost,
			Path:   "/api/v1/identity/login",
			Body:   map[string]string{"email": "", "password": ""},
			Masks:  problemMasks,
		},
		{
			Name:   "identity/register_validation",
			Method: http.MethodPost,
			Path:   "/api/v1/identity/register",
			Body:   map[string]string{"email": "not-an-email", "password": "short"},
			Masks:  problemMasks,
		},
		{
			Name:   "identity/register_ok",
			Method: http.MethodPost,
			Path:   "/api/v1/identity/register",
			Body: map[string]string{
				"email":    freshEmail,
				"password": "parity-pass-1234",
				"name":     "Parity Register",
			},
			Masks: mergeMasks(identityMasksLoginOK, map[string]string{
				"user.email": MaskAnyString,
			}),
		},
		{
			Name:   "identity/register_duplicate_email",
			Method: http.MethodPost,
			Path:   "/api/v1/identity/register",
			Body: map[string]string{
				"email":    "owner@acme.test",
				"password": "whatever-1234",
			},
			Masks: problemMasks,
		},
		{
			Name:   "identity/me_ok",
			Method: http.MethodGet,
			Path:   "/api/v1/identity/me",
			Auth:   true,
			Masks: map[string]string{
				"user.id":   MaskUUID,
				"tenant.id": MaskUUID,
			},
		},
		{
			Name:   "identity/me_unauthorized",
			Method: http.MethodGet,
			Path:   "/api/v1/identity/me",
			Auth:   false,
		},
	}

	for _, c := range cases {
		t.Run(c.Name, func(t *testing.T) {
			if err := r.Run(c); err != nil {
				t.Error(err)
			}
		})
	}
}
