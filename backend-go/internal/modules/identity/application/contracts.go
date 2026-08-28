// Package application, Identity modülünün kullanım senaryosu handler'larını,
// doğrulayıcılarını ve dış dünya sözleşmelerini (DTO) içerir
// (.NET Identity.Application karşılığı). JSON alan adları kablo formatının
// parçasıdır ve frontend api.js ile birebir uyumludur.
package application

import (
	"context"
	"time"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/identity/domain/tenants"
	"pimly.commerslab/backend-go/internal/modules/identity/domain/users"
)

// UserDto, API yanıtlarında dönen kullanıcı özetidir.
type UserDto struct {
	ID    uuid.UUID `json:"id"`
	Email string    `json:"email"`
	Name  string    `json:"name"`
}

// TenantDto, API yanıtlarında dönen tenant özetidir.
type TenantDto struct {
	ID   uuid.UUID `json:"id"`
	Name string    `json:"name"`
}

// LoginResult, başarılı giriş/kayıt yanıt modelidir
// (.NET LoginResult: { token, expires_at, user, tenant }).
type LoginResult struct {
	Token     string    `json:"token"`
	ExpiresAt time.Time `json:"expires_at"`
	User      UserDto   `json:"user"`
	Tenant    TenantDto `json:"tenant"`
}

// MeDto, GET /me yanıt modelidir.
type MeDto struct {
	User   UserDto   `json:"user"`
	Tenant TenantDto `json:"tenant"`
}

// UserRepository, kullanıcı kalıcılık portudur (.NET IUserRepository).
type UserRepository interface {
	// GetByEmail, normalize edilmiş e-postayla kullanıcıyı döner; yoksa nil.
	GetByEmail(ctx context.Context, email string) (*users.User, error)

	// GetByID, kimlikle kullanıcıyı döner; yoksa nil.
	GetByID(ctx context.Context, id uuid.UUID) (*users.User, error)
}

// TenantRepository, tenant kalıcılık portudur (.NET ITenantRepository).
type TenantRepository interface {
	// GetByID, kimlikle tenant'ı döner; yoksa nil.
	GetByID(ctx context.Context, id uuid.UUID) (*tenants.Tenant, error)
}

// MembershipRepository, üyelik kalıcılık portudur (.NET ITenantMembershipRepository).
type MembershipRepository interface {
	// GetPrimaryForUser, kullanıcının birincil üyeliğini döner; yoksa nil.
	GetPrimaryForUser(ctx context.Context, userID uuid.UUID) (*tenants.Membership, error)
}

// RegistrationStore, kayıt akışının tek işlemde (transaction) tenant + kullanıcı
// + üyelik yazma portudur (.NET'te repository Add'leri + IUnitOfWork.SaveChanges
// üçlüsünün karşılığı).
type RegistrationStore interface {
	// CreateRegistration, üç kaydı tek atomik işlemde ekler.
	CreateRegistration(ctx context.Context, tenant *tenants.Tenant, user *users.User, membership *tenants.Membership) error
}

// TokenService, JWT üretim portudur (.NET ITokenService).
type TokenService interface {
	// Generate, kullanıcı ve tenant için imzalı erişim token'ı ve son geçerlilik
	// zamanını üretir.
	Generate(user *users.User, tenant *tenants.Tenant) (token string, expiresAt time.Time, err error)
}
