// Package tenancy, çok kiracılılık (multi-tenancy) için tenant kimliğinin
// taşınma kurallarını tanımlar. .NET tarafındaki ITenantContext /
// AmbientTenantContext / HttpTenantContext üçlüsünün Go karşılığıdır, ancak
// Go'da görünmez EF query filter'ları olmadığı için kural basittir ve serttir:
//
//   - HTTP isteklerinde tenant kimliği JWT'deki tenant_id claim'inden yalnızca
//     auth middleware'inde çıkarılır ve context.Context'e konur.
//   - Worker'larda kuyruk satırı claim edildikten sonra satırın tenant kimliği
//     context'e konur.
//   - Repository metodları tenant kimliğini context'ten OKUMAZ; ctx'ten hemen
//     sonra açık tenantID parametresi alırlar. Context yalnızca taşıma aracıdır,
//     tek okuma noktası handler'dır.
package tenancy

import (
	"context"

	"github.com/google/uuid"
)

// ClaimName, JWT içindeki tenant claim'inin adıdır (.NET TenantClaimTypes.TenantId).
const ClaimName = "tenant_id"

// ctxKey, context çakışmalarını önleyen özel anahtar türüdür.
type ctxKey struct{}

// WithTenant, verilen tenant kimliğini context'e koyar. Boş (uuid.Nil) tenant
// koymak programlama hatasıdır ve panic üretir; .NET AmbientTenantContext'in
// boş GUID reddi ile aynı garantiyi verir.
func WithTenant(ctx context.Context, tenantID uuid.UUID) context.Context {
	if tenantID == uuid.Nil {
		panic("tenancy: boş tenant kimliği context'e konulamaz")
	}
	return context.WithValue(ctx, ctxKey{}, tenantID)
}

// FromContext, context'teki tenant kimliğini döner; yoksa ikinci dönüş değeri
// false olur. Kimliği zorunlu sayan çağrıcılar MustFromContext kullanmalıdır.
func FromContext(ctx context.Context) (uuid.UUID, bool) {
	id, ok := ctx.Value(ctxKey{}).(uuid.UUID)
	return id, ok
}

// MustFromContext, context'teki tenant kimliğini döner; yoksa panic üretir.
// HTTP tarafında auth middleware'i kimliği her zaman koyduğundan, eksikliği
// bir programlama hatasıdır (.NET HttpTenantContext'in throw davranışı).
func MustFromContext(ctx context.Context) uuid.UUID {
	id, ok := FromContext(ctx)
	if !ok {
		panic("tenancy: context'te tenant kimliği yok — auth middleware'i atlanmış")
	}
	return id
}
