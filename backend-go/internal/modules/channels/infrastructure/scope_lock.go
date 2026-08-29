package infrastructure

import (
	"context"
	"fmt"
	"hash/fnv"

	"github.com/jackc/pgx/v5/pgxpool"

	"pimly.commerslab/backend-go/internal/modules/channels/domain"
)

// scopeLockClassID, danışma kilidi ad alanıdır. Postgres'in iki argümanlı
// pg_try_advisory_lock(classid, objid) biçimi kullanılır; sabit classid
// sayesinde listeleme senkronu kilitleri uygulamadaki başka danışma kilidi
// kullanıcılarıyla çakışmaz.
const scopeLockClassID int32 = 0x504C5359 // "PLSY" — Pimly Listing SYnc

// ScopeLocker, kapsam kilidini Postgres danışma kilidiyle uygular.
//
// Neden danışma kilidi: şema değişikliği gerektirmez, ve en önemlisi bağlantı
// düştüğü anda kilit kendiliğinden serbest kalır. Tabloda tutulan bir kiralama
// (lease) kolonu olsaydı, çöken bir worker kapsamı süre dolana kadar kilitli
// bırakır ve o mağazanın senkronu dururdu.
//
// Kilit kapsam (tenant, pazaryeri) düzeyindedir — satır düzeyinde değil.
// Sebebi: pazaryeri hız sınırları zaten satıcı başına işler, yani bir kapsamı
// tek worker'ın işlemesi hem doğruluk hem hız açısından istenen davranıştır.
type ScopeLocker struct {
	pool *pgxpool.Pool
}

// NewScopeLocker, havuzla kapsam kilidi oluşturur.
func NewScopeLocker(pool *pgxpool.Pool) *ScopeLocker {
	return &ScopeLocker{pool: pool}
}

// TryLockScope, kapsamı kilitlemeyi dener; kilit başkasındaysa (nil, false, nil)
// döner. Kilit alındığında bir havuz bağlantısı release çağrılana kadar
// tutulur — bu yüzden çağıran taraf kapsam sayısını sınırlı tutmalıdır.
func (l *ScopeLocker) TryLockScope(ctx context.Context, scope domain.ListingSyncScope) (func(), bool, error) {
	conn, err := l.pool.Acquire(ctx)
	if err != nil {
		return nil, false, fmt.Errorf("channels: kilit bağlantısı alınamadı: %w", err)
	}

	key := scopeLockKey(scope)
	var locked bool
	if err := conn.QueryRow(ctx,
		`SELECT pg_try_advisory_lock($1, $2)`, scopeLockClassID, key).Scan(&locked); err != nil {
		conn.Release()
		return nil, false, fmt.Errorf("channels: kapsam kilidi denenemedi: %w", err)
	}
	if !locked {
		conn.Release()
		return nil, false, nil
	}

	release := func() {
		// Kilit, çağıranın ctx'i iptal olmuş olsa bile bırakılmalıdır; aksi
		// halde kapanış sırasında kilit bağlantıyla birlikte havuza kirli döner.
		_, _ = conn.Exec(context.WithoutCancel(ctx),
			`SELECT pg_advisory_unlock($1, $2)`, scopeLockClassID, key)
		conn.Release()
	}
	return release, true, nil
}

// scopeLockKey, kapsamı kararlı bir int32 anahtara indirger. Çakışma yalnızca
// iki farklı kapsamın aynı anda işlenememesine yol açar (bir sonraki turda
// yeniden denenir), veri bütünlüğünü bozmaz.
func scopeLockKey(scope domain.ListingSyncScope) int32 {
	h := fnv.New32a()
	idBytes := scope.TenantID
	_, _ = h.Write(idBytes[:])
	_, _ = h.Write([]byte{'|'})
	_, _ = h.Write([]byte(scope.MarketplaceCode))
	return int32(h.Sum32())
}
