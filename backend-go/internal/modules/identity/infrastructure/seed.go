package infrastructure

import (
	"context"
	"log/slog"

	"pimly.commerslab/backend-go/internal/modules/identity/application"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// SeedDevUser, geliştirme ortamı için sabit kullanıcıyı gerçek kayıt akışıyla
// oluşturur (.NET IdentitySeedExtensions karşılığı): owner@acme.test / demo1234,
// tenant "Acme". Kullanıcı zaten varsa çakışma sessizce yutulur (idempotent).
// Yalnızca Development ortamında çağrılmalıdır.
func SeedDevUser(ctx context.Context, register *application.RegisterUserHandler) error {
	tenantName := "Acme"
	result := register.Execute(ctx, application.RegisterUserCommand{
		Email:      "owner@acme.test",
		Password:   "demo1234",
		Name:       "Acme Owner",
		TenantName: &tenantName,
	})
	if result.IsFailure() {
		if result.Err().Code == sharedkernel.ErrorCodeConflict {
			return nil
		}
		return result.Err()
	}
	slog.Info("Seeded development user {Email} with tenant {TenantName}.",
		slog.String("Email", "owner@acme.test"),
		slog.String("TenantName", tenantName))
	return nil
}
