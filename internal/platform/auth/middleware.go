package auth

import (
	"context"
	"net/http"
	"strings"

	"github.com/google/uuid"

	"github.com/kazimcavus/pimly/internal/platform/httpx"
	"github.com/kazimcavus/pimly/internal/platform/tenant"
	"github.com/kazimcavus/pimly/internal/shared/apperr"
)

type userCtxKey struct{}

// UserID extracts the authenticated user id from ctx.
func UserID(ctx context.Context) (uuid.UUID, bool) {
	id, ok := ctx.Value(userCtxKey{}).(uuid.UUID)
	return id, ok
}

func withUser(ctx context.Context, id uuid.UUID) context.Context {
	return context.WithValue(ctx, userCtxKey{}, id)
}

// Authenticate verifies the bearer token, then populates the request context
// with the user id and the tenant resolved at login time. Downstream handlers
// scope DB access via tenant.FromContext + db.WithTenant.
func (s *Service) Authenticate(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		raw := bearerToken(r)
		if raw == "" {
			httpx.Error(w, r, apperr.Unauthorized("missing bearer token"))
			return
		}
		claims, err := s.Parse(raw)
		if err != nil {
			httpx.Error(w, r, err)
			return
		}
		userID, err := uuid.Parse(claims.Subject)
		if err != nil {
			httpx.Error(w, r, apperr.Unauthorized("invalid token subject"))
			return
		}
		t := tenant.Tenant{
			ID:          claims.TenantID,
			Slug:        claims.TenantSlug,
			SchemaName:  claims.SchemaName,
			Role:        claims.Role,
			BarcodeCode: claims.BarcodeCode,
		}
		if err := tenant.ValidateSchemaName(t.SchemaName); err != nil {
			httpx.Error(w, r, apperr.Unauthorized("invalid tenant in token"))
			return
		}
		ctx := withUser(tenant.NewContext(r.Context(), t), userID)
		next.ServeHTTP(w, r.WithContext(ctx))
	})
}

// RequireRole returns middleware that allows the request only if the active
// tenant role is one of roles. (Role guard skeleton — owner is full access;
// staff/readonly are placeholders refined in later milestones.)
func RequireRole(roles ...string) func(http.Handler) http.Handler {
	return func(next http.Handler) http.Handler {
		return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			t, ok := tenant.FromContext(r.Context())
			if !ok {
				httpx.Error(w, r, apperr.Unauthorized("not authenticated"))
				return
			}
			for _, role := range roles {
				if t.Role == role {
					next.ServeHTTP(w, r)
					return
				}
			}
			httpx.Error(w, r, apperr.Forbidden("requires role %v", roles))
		})
	}
}

func bearerToken(r *http.Request) string {
	h := r.Header.Get("Authorization")
	if h == "" {
		return ""
	}
	const prefix = "Bearer "
	if len(h) > len(prefix) && strings.EqualFold(h[:len(prefix)], prefix) {
		return strings.TrimSpace(h[len(prefix):])
	}
	return ""
}
