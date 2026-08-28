package users

import (
	"strings"
	"time"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// User, kimlik doğrulama için kullanıcı kök varlığıdır
// (.NET Identity.Domain.Users.User karşılığı).
type User struct {
	// ID, kullanıcının benzersiz kimliğidir.
	ID uuid.UUID

	// Email, kullanıcının normalize edilmiş (küçük harf) benzersiz e-posta adresidir.
	Email string

	// PasswordHash, ASP.NET Identity V3 biçiminde şifre özetidir.
	PasswordHash string

	// Name, kullanıcının görünen adıdır; boş olabilir.
	Name string

	// CreatedAt, kayıt oluşturulma zamanıdır (UTC).
	CreatedAt time.Time
}

// NewUser, doğrulanmış yeni bir kullanıcı oluşturur: e-posta kırpılıp küçük
// harfe çevrilir, ad kırpılır. Hata mesajları .NET karşılığıyla birebir aynıdır.
func NewUser(email, passwordHash, name string) sharedkernel.ResultOf[*User] {
	if strings.TrimSpace(email) == "" {
		return sharedkernel.FailOf[*User](sharedkernel.NewValidationError("Email is required."))
	}
	return sharedkernel.OkOf(&User{
		ID:           uuid.New(),
		Email:        strings.ToLower(strings.TrimSpace(email)),
		PasswordHash: passwordHash,
		Name:         strings.TrimSpace(name),
		CreatedAt:    time.Now().UTC(),
	})
}
