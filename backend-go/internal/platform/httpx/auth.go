package httpx

import (
	"context"
	"net/http"
	"strings"

	"github.com/golang-jwt/jwt/v5"
	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/sharedkernel/tenancy"
)

// authClaimsKey, doğrulanmış kullanıcı kimliğini (uuid) taşıyan context anahtarıdır.
type authClaimsKey struct{}

// AuthUserID, JWT auth middleware'inin doğruladığı kullanıcı kimliğini döner;
// middleware'den geçmemiş isteklerde ikinci dönüş değeri false olur.
func AuthUserID(ctx context.Context) (uuid.UUID, bool) {
	id, ok := ctx.Value(authClaimsKey{}).(uuid.UUID)
	return id, ok
}

// JWTAuth, Bearer token doğrulayan middleware üretir (.NET AddJwtBearer +
// RequireAuthorization karşılığı). Yalnızca imza (HS256) ve geçerlilik süresi
// denetlenir; iss/aud .NET tarafıyla uyumlu olarak denetlenmez. Başarılı
// doğrulamada kullanıcı kimliği ve tenant kimliği context'e konur; başarısızlıkta
// .NET gibi gövdesiz 401 döner.
func JWTAuth(secret string) func(http.Handler) http.Handler {
	key := []byte(secret)
	parser := jwt.NewParser(
		jwt.WithValidMethods([]string{jwt.SigningMethodHS256.Alg()}),
		jwt.WithExpirationRequired(),
	)
	return func(next http.Handler) http.Handler {
		return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			raw, ok := bearerToken(r)
			if !ok {
				unauthorized(w)
				return
			}
			claims := jwt.MapClaims{}
			if _, err := parser.ParseWithClaims(raw, claims, func(*jwt.Token) (any, error) { return key, nil }); err != nil {
				unauthorized(w)
				return
			}

			userID, uerr := claimUUID(claims, "sub")
			tenantID, terr := claimUUID(claims, tenancy.ClaimName)
			if uerr != nil || terr != nil {
				unauthorized(w)
				return
			}

			ctx := context.WithValue(r.Context(), authClaimsKey{}, userID)
			ctx = WithUserID(ctx, userID.String())
			ctx = tenancy.WithTenant(ctx, tenantID)
			next.ServeHTTP(w, r.WithContext(ctx))
		})
	}
}

// bearerToken, Authorization başlığından Bearer token'ı çıkarır.
func bearerToken(r *http.Request) (string, bool) {
	header := r.Header.Get("Authorization")
	const prefix = "Bearer "
	if !strings.HasPrefix(header, prefix) {
		return "", false
	}
	return header[len(prefix):], true
}

// claimUUID, verilen claim'i UUID olarak çözer.
func claimUUID(claims jwt.MapClaims, name string) (uuid.UUID, error) {
	raw, _ := claims[name].(string)
	return uuid.Parse(raw)
}

// unauthorized, .NET'in RequireAuthorization davranışıyla uyumlu gövdesiz 401 yazar.
func unauthorized(w http.ResponseWriter) {
	w.Header().Set("WWW-Authenticate", "Bearer")
	w.WriteHeader(http.StatusUnauthorized)
}
