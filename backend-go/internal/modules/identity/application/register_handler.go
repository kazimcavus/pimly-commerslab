package application

import (
	"context"
	"strings"
	"time"

	"pimly.commerslab/backend-go/internal/modules/identity/domain/tenants"
	"pimly.commerslab/backend-go/internal/modules/identity/domain/users"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// RegisterUserCommand, kayıt isteğini taşır. TenantName nil olabilir
// (JSON'da alan hiç gönderilmemiş demektir; boş dizgi ayrı doğrulanır).
type RegisterUserCommand struct {
	Email      string
	Password   string
	Name       string
	TenantName *string
}

// RegisterUserHandler, kayıt sırasında kullanıcı + tenant + birincil üyeliği
// TEK atomik işlemde oluşturur ve otomatik giriş yapar
// (.NET RegisterUserHandler karşılığı). Her kayıt yeni bir tenant açar;
// mevcut tenant'a davet akışı yoktur.
type RegisterUserHandler struct {
	users  UserRepository
	store  RegistrationStore
	tokens TokenService
	now    func() time.Time
}

// NewRegisterUserHandler, bağımlılıklarıyla yeni bir kayıt handler'ı oluşturur.
// now nil verilirse gerçek saat kullanılır (testler sabit saat enjekte eder).
func NewRegisterUserHandler(users UserRepository, store RegistrationStore, tokens TokenService, now func() time.Time) *RegisterUserHandler {
	if now == nil {
		now = func() time.Time { return time.Now().UTC() }
	}
	return &RegisterUserHandler{users: users, store: store, tokens: tokens, now: now}
}

// Execute, kayıt komutunu işler ve başarılıysa otomatik girişin LoginResult'ını döner.
func (h *RegisterUserHandler) Execute(ctx context.Context, cmd RegisterUserCommand) sharedkernel.ResultOf[LoginResult] {
	if verr := ValidateRegisterUserCommand(cmd); verr != nil {
		return sharedkernel.FailOf[LoginResult](verr)
	}

	normalizedEmail := strings.ToLower(strings.TrimSpace(cmd.Email))
	existing, err := h.users.GetByEmail(ctx, normalizedEmail)
	if err != nil {
		return sharedkernel.FailOf[LoginResult](sharedkernel.NewInternalError(err.Error()))
	}
	if existing != nil {
		return sharedkernel.FailOf[LoginResult](sharedkernel.NewConflictError("Email is already registered."))
	}

	now := h.now()
	tenantResult := tenants.NewTenant(resolveTenantName(cmd, normalizedEmail), now)
	if tenantResult.IsFailure() {
		return sharedkernel.FailOf[LoginResult](tenantResult.Err())
	}
	tenant := tenantResult.Value()

	passwordHash, err := users.HashPassword(cmd.Password)
	if err != nil {
		return sharedkernel.FailOf[LoginResult](sharedkernel.NewInternalError(err.Error()))
	}
	userResult := users.NewUser(normalizedEmail, passwordHash, cmd.Name)
	if userResult.IsFailure() {
		return sharedkernel.FailOf[LoginResult](userResult.Err())
	}
	user := userResult.Value()

	membershipResult := tenants.NewMembership(tenant.ID, user.ID, true, now)
	if membershipResult.IsFailure() {
		return sharedkernel.FailOf[LoginResult](membershipResult.Err())
	}

	if err := h.store.CreateRegistration(ctx, tenant, user, membershipResult.Value()); err != nil {
		return sharedkernel.FailOf[LoginResult](sharedkernel.NewInternalError(err.Error()))
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

// resolveTenantName, tenant adını .NET ile aynı öncelik sırasıyla belirler:
// TenantName → Name → e-postanın yerel kısmı.
func resolveTenantName(cmd RegisterUserCommand, normalizedEmail string) string {
	if cmd.TenantName != nil && strings.TrimSpace(*cmd.TenantName) != "" {
		return strings.TrimSpace(*cmd.TenantName)
	}
	if strings.TrimSpace(cmd.Name) != "" {
		return strings.TrimSpace(cmd.Name)
	}
	local, _, _ := strings.Cut(normalizedEmail, "@")
	return local
}
