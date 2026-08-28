// Package infrastructure, Identity modülünün kalıcılık ve token altyapısını
// içerir (.NET Identity.Infrastructure karşılığı): pgx tabanlı repository'ler,
// HS256 JWT servisi ve geliştirme ortamı seed'i. Identity şeması tenant'ları
// TANIMLAYAN modül olduğundan sorgularında tenant_id filtresi yoktur.
package infrastructure

import (
	"context"
	"errors"
	"fmt"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgxpool"

	"pimly.commerslab/backend-go/internal/modules/identity/domain/tenants"
	"pimly.commerslab/backend-go/internal/modules/identity/domain/users"
)

// Store, Identity şemasının tüm kalıcılık portlarını tek yapıda uygular:
// application.UserRepository, TenantRepository, MembershipRepository ve
// RegistrationStore. Tablolar küçük ve akış tek olduğundan ayrı repository
// struct'ları yerine tek Store yeterlidir.
type Store struct {
	pool *pgxpool.Pool
}

// NewStore, verilen havuzla Identity kalıcılık deposunu oluşturur.
func NewStore(pool *pgxpool.Pool) *Store { return &Store{pool: pool} }

// GetByEmail, normalize edilmiş e-postayla kullanıcıyı döner; yoksa nil.
func (s *Store) GetByEmail(ctx context.Context, email string) (*users.User, error) {
	const query = `SELECT id, email, password_hash, name, created_at
	               FROM identity.users WHERE email = $1`
	return scanUser(s.pool.QueryRow(ctx, query, email))
}

// GetByID, kimlikle kullanıcıyı döner; yoksa nil.
func (s *Store) GetByID(ctx context.Context, id uuid.UUID) (*users.User, error) {
	const query = `SELECT id, email, password_hash, name, created_at
	               FROM identity.users WHERE id = $1`
	return scanUser(s.pool.QueryRow(ctx, query, id))
}

// scanUser, tek kullanıcı satırını okur; satır yoksa (nil, nil) döner.
func scanUser(row pgx.Row) (*users.User, error) {
	var u users.User
	err := row.Scan(&u.ID, &u.Email, &u.PasswordHash, &u.Name, &u.CreatedAt)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("identity: kullanıcı okunamadı: %w", err)
	}
	return &u, nil
}

// GetTenantByID, kimlikle tenant'ı döner; yoksa nil.
// (application.TenantRepository.GetByID bağlaması için ayrı ada sahiptir;
// bkz. TenantRepo sarmalayıcısı.)
func (s *Store) GetTenantByID(ctx context.Context, id uuid.UUID) (*tenants.Tenant, error) {
	const query = `SELECT id, name, created_at FROM identity.tenants WHERE id = $1`
	var t tenants.Tenant
	err := s.pool.QueryRow(ctx, query, id).Scan(&t.ID, &t.Name, &t.CreatedAt)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("identity: tenant okunamadı: %w", err)
	}
	return &t, nil
}

// GetPrimaryForUser, kullanıcının birincil üyeliğini döner; yoksa nil.
func (s *Store) GetPrimaryForUser(ctx context.Context, userID uuid.UUID) (*tenants.Membership, error) {
	const query = `SELECT id, tenant_id, user_id, is_primary, joined_at
	               FROM identity.tenant_memberships
	               WHERE user_id = $1 AND is_primary = TRUE`
	var m tenants.Membership
	err := s.pool.QueryRow(ctx, query, userID).Scan(&m.ID, &m.TenantID, &m.UserID, &m.IsPrimary, &m.JoinedAt)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("identity: üyelik okunamadı: %w", err)
	}
	return &m, nil
}

// CreateRegistration, tenant + kullanıcı + üyeliği TEK transaction'da ekler
// (.NET'teki üç Add + SaveChanges karşılığı — kayıt yarım kalamaz).
func (s *Store) CreateRegistration(ctx context.Context, tenant *tenants.Tenant, user *users.User, membership *tenants.Membership) error {
	tx, err := s.pool.Begin(ctx)
	if err != nil {
		return fmt.Errorf("identity: kayıt işlemi başlatılamadı: %w", err)
	}
	defer func() { _ = tx.Rollback(ctx) }()

	if _, err := tx.Exec(ctx,
		`INSERT INTO identity.tenants (id, name, created_at) VALUES ($1, $2, $3)`,
		tenant.ID, tenant.Name, tenant.CreatedAt); err != nil {
		return fmt.Errorf("identity: tenant eklenemedi: %w", err)
	}
	if _, err := tx.Exec(ctx,
		`INSERT INTO identity.users (id, email, password_hash, name, created_at)
		 VALUES ($1, $2, $3, $4, $5)`,
		user.ID, user.Email, user.PasswordHash, user.Name, user.CreatedAt); err != nil {
		return fmt.Errorf("identity: kullanıcı eklenemedi: %w", err)
	}
	if _, err := tx.Exec(ctx,
		`INSERT INTO identity.tenant_memberships (id, tenant_id, user_id, is_primary, joined_at)
		 VALUES ($1, $2, $3, $4, $5)`,
		membership.ID, membership.TenantID, membership.UserID, membership.IsPrimary, membership.JoinedAt); err != nil {
		return fmt.Errorf("identity: üyelik eklenemedi: %w", err)
	}
	return tx.Commit(ctx)
}

// TenantRepo, Store'u application.TenantRepository portuna uyarlayan ince
// sarmalayıcıdır (GetByID ad çakışmasını çözer: Store.GetByID kullanıcı içindir).
type TenantRepo struct{ *Store }

// GetByID, kimlikle tenant'ı döner; yoksa nil.
func (r TenantRepo) GetByID(ctx context.Context, id uuid.UUID) (*tenants.Tenant, error) {
	return r.GetTenantByID(ctx, id)
}
