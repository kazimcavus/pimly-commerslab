// Package tenants, Identity modülünün tenant (müşteri organizasyonu) ve
// üyelik varlıklarını içerir (.NET Identity.Domain.Tenants karşılığı).
package tenants

import (
	"strings"
	"time"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// Tenant, Pimly SaaS müşteri organizasyonudur.
type Tenant struct {
	// ID, tenant'ın benzersiz kimliğidir.
	ID uuid.UUID

	// Name, organizasyon adıdır (en çok 200 karakter).
	Name string

	// CreatedAt, oluşturulma zamanıdır (UTC).
	CreatedAt time.Time
}

// NewTenant, doğrulanmış yeni bir tenant oluşturur. Hata mesajları .NET
// karşılığıyla birebir aynıdır.
func NewTenant(name string, createdAt time.Time) sharedkernel.ResultOf[*Tenant] {
	trimmed := strings.TrimSpace(name)
	if trimmed == "" {
		return sharedkernel.FailOf[*Tenant](sharedkernel.NewValidationError("Tenant name is required."))
	}
	if len([]rune(trimmed)) > 200 {
		return sharedkernel.FailOf[*Tenant](sharedkernel.NewValidationError("Tenant name cannot exceed 200 characters."))
	}
	return sharedkernel.OkOf(&Tenant{ID: uuid.New(), Name: trimmed, CreatedAt: createdAt})
}

// Membership, kullanıcı ↔ tenant üyelik ilişkisidir. IsPrimary, girişte
// kullanılan varsayılan tenant'ı işaretler; bugünkü auth akışı yalnızca
// birincil üyeliği kullanır.
type Membership struct {
	// ID, üyeliğin benzersiz kimliğidir.
	ID uuid.UUID

	// TenantID, üyeliğin ait olduğu tenant'tır.
	TenantID uuid.UUID

	// UserID, üyeliğin sahibi kullanıcıdır.
	UserID uuid.UUID

	// IsPrimary, girişte varsayılan tenant olup olmadığını belirtir.
	IsPrimary bool

	// JoinedAt, üyelik başlangıç zamanıdır (UTC).
	JoinedAt time.Time
}

// NewMembership, doğrulanmış yeni bir üyelik oluşturur.
func NewMembership(tenantID, userID uuid.UUID, isPrimary bool, joinedAt time.Time) sharedkernel.ResultOf[*Membership] {
	if tenantID == uuid.Nil {
		return sharedkernel.FailOf[*Membership](sharedkernel.NewValidationError("Tenant id is required."))
	}
	if userID == uuid.Nil {
		return sharedkernel.FailOf[*Membership](sharedkernel.NewValidationError("User id is required."))
	}
	return sharedkernel.OkOf(&Membership{
		ID:        uuid.New(),
		TenantID:  tenantID,
		UserID:    userID,
		IsPrimary: isPrimary,
		JoinedAt:  joinedAt,
	})
}
