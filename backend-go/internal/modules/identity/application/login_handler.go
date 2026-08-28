package application

import (
	"context"
	"strings"

	"pimly.commerslab/backend-go/internal/modules/identity/domain/users"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// LoginCommand, kullanıcı giriş isteğini taşır.
type LoginCommand struct {
	Email    string
	Password string
}

// LoginHandler, kullanıcı giriş işlemini yürütür (.NET LoginHandler karşılığı):
// doğrulama → kullanıcı arama → şifre doğrulama → birincil üyelik → tenant →
// token üretimi. Kimlik bilgisi hatalarının tümü aynı "Invalid credentials."
// yanıtına eşlenir ki e-posta varlığı sızdırılmasın.
type LoginHandler struct {
	users       UserRepository
	memberships MembershipRepository
	tenants     TenantRepository
	tokens      TokenService
}

// NewLoginHandler, bağımlılıklarıyla yeni bir giriş handler'ı oluşturur.
func NewLoginHandler(users UserRepository, memberships MembershipRepository, tenants TenantRepository, tokens TokenService) *LoginHandler {
	return &LoginHandler{users: users, memberships: memberships, tenants: tenants, tokens: tokens}
}

// Execute, giriş komutunu işler ve başarılıysa LoginResult döner.
func (h *LoginHandler) Execute(ctx context.Context, cmd LoginCommand) sharedkernel.ResultOf[LoginResult] {
	if verr := ValidateLoginCommand(cmd); verr != nil {
		return sharedkernel.FailOf[LoginResult](verr)
	}

	user, err := h.users.GetByEmail(ctx, strings.ToLower(strings.TrimSpace(cmd.Email)))
	if err != nil {
		return sharedkernel.FailOf[LoginResult](sharedkernel.NewInternalError(err.Error()))
	}
	if user == nil || !users.VerifyPassword(cmd.Password, user.PasswordHash) {
		return sharedkernel.FailOf[LoginResult](sharedkernel.NewUnauthorizedError("Invalid credentials."))
	}

	membership, err := h.memberships.GetPrimaryForUser(ctx, user.ID)
	if err != nil {
		return sharedkernel.FailOf[LoginResult](sharedkernel.NewInternalError(err.Error()))
	}
	if membership == nil {
		return sharedkernel.FailOf[LoginResult](sharedkernel.NewUnauthorizedError("User is not assigned to a tenant."))
	}

	tenant, err := h.tenants.GetByID(ctx, membership.TenantID)
	if err != nil {
		return sharedkernel.FailOf[LoginResult](sharedkernel.NewInternalError(err.Error()))
	}
	if tenant == nil {
		return sharedkernel.FailOf[LoginResult](sharedkernel.NewUnauthorizedError("Tenant not found."))
	}

	token, expiresAt, err := h.tokens.Generate(user, tenant)
	if err != nil {
		return sharedkernel.FailOf[LoginResult](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(LoginResult{
		Token:     token,
		ExpiresAt: expiresAt,
		User:      UserDto{ID: user.ID, Email: user.Email, Name: user.Name},
		Tenant:    TenantDto{ID: tenant.ID, Name: tenant.Name},
	})
}
