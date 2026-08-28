package application

import (
	"context"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// GetMeQuery, aktif kullanıcı sorgusunu taşır; kimlikler JWT claim'lerinden gelir.
type GetMeQuery struct {
	UserID   uuid.UUID
	TenantID uuid.UUID
}

// GetMeHandler, aktif kullanıcı ve tenant bilgisini getirir
// (.NET GetMeHandler karşılığı). Token'daki tenant, kullanıcının birincil
// üyeliğiyle eşleşmiyorsa erişim reddedilir.
type GetMeHandler struct {
	users       UserRepository
	tenants     TenantRepository
	memberships MembershipRepository
}

// NewGetMeHandler, bağımlılıklarıyla yeni bir handler oluşturur.
func NewGetMeHandler(users UserRepository, tenants TenantRepository, memberships MembershipRepository) *GetMeHandler {
	return &GetMeHandler{users: users, tenants: tenants, memberships: memberships}
}

// Execute, sorguyu işler ve başarılıysa MeDto döner.
func (h *GetMeHandler) Execute(ctx context.Context, query GetMeQuery) sharedkernel.ResultOf[MeDto] {
	user, err := h.users.GetByID(ctx, query.UserID)
	if err != nil {
		return sharedkernel.FailOf[MeDto](sharedkernel.NewInternalError(err.Error()))
	}
	if user == nil {
		return sharedkernel.FailOf[MeDto](sharedkernel.NewNotFoundError("User not found."))
	}

	membership, err := h.memberships.GetPrimaryForUser(ctx, user.ID)
	if err != nil {
		return sharedkernel.FailOf[MeDto](sharedkernel.NewInternalError(err.Error()))
	}
	if membership == nil || membership.TenantID != query.TenantID {
		return sharedkernel.FailOf[MeDto](sharedkernel.NewUnauthorizedError("Tenant access denied."))
	}

	tenant, err := h.tenants.GetByID(ctx, query.TenantID)
	if err != nil {
		return sharedkernel.FailOf[MeDto](sharedkernel.NewInternalError(err.Error()))
	}
	if tenant == nil {
		return sharedkernel.FailOf[MeDto](sharedkernel.NewNotFoundError("Tenant not found."))
	}

	return sharedkernel.OkOf(MeDto{
		User:   UserDto{ID: user.ID, Email: user.Email, Name: user.Name},
		Tenant: TenantDto{ID: tenant.ID, Name: tenant.Name},
	})
}
