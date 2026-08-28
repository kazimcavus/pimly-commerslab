package infrastructure

import (
	"fmt"
	"time"

	"github.com/golang-jwt/jwt/v5"

	"pimly.commerslab/backend-go/internal/modules/identity/domain/tenants"
	"pimly.commerslab/backend-go/internal/modules/identity/domain/users"
	"pimly.commerslab/backend-go/internal/sharedkernel/tenancy"
)

// JwtTokenService, HS256 imzalı JWT üretir (.NET JwtTokenService karşılığı).
// Claim'ler: sub (kullanıcı kimliği), email, tenant_id; iss/aud kullanılmaz
// (.NET doğrulaması da bunları denetlemez — iki taraf birbirinin token'ını kabul eder).
type JwtTokenService struct {
	secret          []byte
	expirationHours int
}

// NewJwtTokenService, verilen gizli anahtar ve geçerlilik süresiyle servis oluşturur.
func NewJwtTokenService(secret string, expirationHours int) *JwtTokenService {
	return &JwtTokenService{secret: []byte(secret), expirationHours: expirationHours}
}

// Generate, kullanıcı ve tenant için imzalı erişim token'ı üretir.
func (s *JwtTokenService) Generate(user *users.User, tenant *tenants.Tenant) (string, time.Time, error) {
	now := time.Now().UTC()
	expiresAt := now.Add(time.Duration(s.expirationHours) * time.Hour)

	claims := jwt.MapClaims{
		"sub":            user.ID.String(),
		"email":          user.Email,
		tenancy.ClaimName: tenant.ID.String(),
		"exp":            expiresAt.Unix(),
		"nbf":            now.Unix(),
		"iat":            now.Unix(),
	}
	token, err := jwt.NewWithClaims(jwt.SigningMethodHS256, claims).SignedString(s.secret)
	if err != nil {
		return "", time.Time{}, fmt.Errorf("identity: token imzalanamadı: %w", err)
	}
	return token, expiresAt, nil
}
