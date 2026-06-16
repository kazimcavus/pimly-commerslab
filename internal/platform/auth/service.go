package auth

import (
	"context"
	"errors"
	"time"

	"github.com/golang-jwt/jwt/v5"
	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"

	"github.com/kazimcavus/pimly/internal/platform/db"
	"github.com/kazimcavus/pimly/internal/platform/db/globaldb"
	"github.com/kazimcavus/pimly/internal/platform/tenant"
	"github.com/kazimcavus/pimly/internal/shared/apperr"
)

// Claims is the JWT payload. The active tenant is resolved from the user's
// membership at login and embedded here (signed), so each request carries its
// tenant without an extra lookup.
type Claims struct {
	TenantID   uuid.UUID `json:"tid"`
	TenantSlug string    `json:"tslug"`
	SchemaName string    `json:"schema"`
	Role       string    `json:"role"`
	jwt.RegisteredClaims
}

// Service issues and verifies tokens and performs login.
type Service struct {
	db     *db.DB
	secret []byte
	ttl    time.Duration
}

// NewService builds an auth Service. secret signs HS256 tokens.
func NewService(database *db.DB, secret string, ttl time.Duration) *Service {
	if ttl <= 0 {
		ttl = 24 * time.Hour
	}
	return &Service{db: database, secret: []byte(secret), ttl: ttl}
}

// LoginResult is returned on successful authentication.
type LoginResult struct {
	Token     string        `json:"token"`
	ExpiresAt time.Time     `json:"expires_at"`
	User      globaldb.User `json:"-"`
	Tenant    tenant.Tenant `json:"-"`
}

// Login verifies credentials and resolves the active tenant from the user's
// membership (the requested tenantSlug if given and the user is a member,
// otherwise their first membership), then issues a token.
func (s *Service) Login(ctx context.Context, email, password, tenantSlug string) (*LoginResult, error) {
	gq := globaldb.New(s.db.Pool())

	user, err := gq.GetUserByEmail(ctx, email)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, apperr.Unauthorized("invalid credentials")
	} else if err != nil {
		return nil, apperr.Internal(err)
	}

	ok, err := VerifyPassword(password, user.PasswordHash)
	if err != nil || !ok {
		return nil, apperr.Unauthorized("invalid credentials")
	}

	var (
		tnt  globaldb.Tenant
		role string
	)
	if tenantSlug != "" {
		tnt, err = gq.GetTenantBySlug(ctx, tenantSlug)
		if errors.Is(err, pgx.ErrNoRows) {
			return nil, apperr.Forbidden("not a member of %q", tenantSlug)
		} else if err != nil {
			return nil, apperr.Internal(err)
		}
		m, err := gq.GetMembershipByUserAndTenant(ctx, globaldb.GetMembershipByUserAndTenantParams{UserID: user.ID, TenantID: tnt.ID})
		if errors.Is(err, pgx.ErrNoRows) {
			return nil, apperr.Forbidden("not a member of %q", tenantSlug)
		} else if err != nil {
			return nil, apperr.Internal(err)
		}
		role = m.Role
	} else {
		m, err := gq.GetFirstMembershipForUser(ctx, user.ID)
		if errors.Is(err, pgx.ErrNoRows) {
			return nil, apperr.Forbidden("user has no tenant membership")
		} else if err != nil {
			return nil, apperr.Internal(err)
		}
		tnt, err = gq.GetTenantByID(ctx, m.TenantID)
		if err != nil {
			return nil, apperr.Internal(err)
		}
		role = m.Role
	}

	t := tenant.Tenant{ID: tnt.ID, Slug: tnt.Slug, SchemaName: tnt.SchemaName, Role: role}
	token, exp, err := s.Issue(user.ID, t)
	if err != nil {
		return nil, apperr.Internal(err)
	}
	return &LoginResult{Token: token, ExpiresAt: exp, User: user, Tenant: t}, nil
}

// Issue signs a token for the given user scoped to tenant t.
func (s *Service) Issue(userID uuid.UUID, t tenant.Tenant) (string, time.Time, error) {
	now := time.Now()
	exp := now.Add(s.ttl)
	claims := Claims{
		TenantID:   t.ID,
		TenantSlug: t.Slug,
		SchemaName: t.SchemaName,
		Role:       t.Role,
		RegisteredClaims: jwt.RegisteredClaims{
			Subject:   userID.String(),
			IssuedAt:  jwt.NewNumericDate(now),
			ExpiresAt: jwt.NewNumericDate(exp),
		},
	}
	tok := jwt.NewWithClaims(jwt.SigningMethodHS256, claims)
	signed, err := tok.SignedString(s.secret)
	if err != nil {
		return "", time.Time{}, err
	}
	return signed, exp, nil
}

// Parse verifies a token and returns its claims.
func (s *Service) Parse(tokenStr string) (*Claims, error) {
	claims := &Claims{}
	_, err := jwt.ParseWithClaims(tokenStr, claims, func(t *jwt.Token) (any, error) {
		return s.secret, nil
	}, jwt.WithValidMethods([]string{"HS256"}))
	if err != nil {
		return nil, apperr.Unauthorized("invalid token")
	}
	return claims, nil
}
