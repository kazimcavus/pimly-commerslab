package listingsync

import (
	"context"
	"sync"
	"testing"
	"time"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/channels/domain"
)

// schedulerStore, yalnızca zamanlayıcıyı sınamak için kapsam listesi döner ve
// kirli listeleme döndürmez — böylece senkronlayıcılar hemen boş özetle çıkar
// ve test ağ/gateway kurulumu gerektirmez.
type schedulerStore struct {
	mockStore
	scopes []domain.ListingSyncScope
}

func (s *schedulerStore) ListDirtyScopes(context.Context, []uuid.UUID, time.Time) ([]domain.ListingSyncScope, error) {
	return s.scopes, nil
}

func (s *schedulerStore) ListDirty(context.Context, uuid.UUID, string, time.Time, int) ([]*domain.ProductListing, error) {
	return nil, nil
}

// countingLocker, kilit çağrılarını sayar; granted kilidin verilip
// verilmeyeceğini belirler.
type countingLocker struct {
	mu        sync.Mutex
	granted   bool
	attempts  map[domain.ListingSyncScope]int
	releases  int
	maxActive int
	active    int
}

func newCountingLocker(granted bool) *countingLocker {
	return &countingLocker{granted: granted, attempts: map[domain.ListingSyncScope]int{}}
}

func (l *countingLocker) TryLockScope(_ context.Context, scope domain.ListingSyncScope) (func(), bool, error) {
	l.mu.Lock()
	l.attempts[scope]++
	if !l.granted {
		l.mu.Unlock()
		return nil, false, nil
	}
	l.active++
	if l.active > l.maxActive {
		l.maxActive = l.active
	}
	l.mu.Unlock()

	// Kısa bir bekleme, eşzamanlı kapsamların gerçekten örtüşmesini sağlar;
	// aksi halde havuz sıralı çalışsa da test geçerdi.
	time.Sleep(2 * time.Millisecond)

	return func() {
		l.mu.Lock()
		defer l.mu.Unlock()
		l.active--
		l.releases++
	}, true, nil
}

func makeScopes(n int) []domain.ListingSyncScope {
	scopes := make([]domain.ListingSyncScope, n)
	for i := range scopes {
		scopes[i] = domain.ListingSyncScope{TenantID: uuid.New(), MarketplaceCode: "TY"}
	}
	return scopes
}

func newTestRunner(store Store) *Runner {
	offer := NewOfferSyncer(store, nil, nil, nil)
	content := NewContentSyncer(store, nil, nil, nil, NewAssembler(store), nil)
	return NewRunner(store, offer, content)
}

// TestRunOnce_VisitsEveryScopeExactlyOnce, paralel dağıtımın hiçbir kapsamı
// düşürmediğini ve tekrarlamadığını doğrular — kanal/WaitGroup kodundaki asıl risk.
func TestRunOnce_VisitsEveryScopeExactlyOnce(t *testing.T) {
	for _, concurrency := range []int{1, 2, 8, 64} {
		store := &schedulerStore{scopes: makeScopes(25)}
		locker := newCountingLocker(false)
		runner := newTestRunner(store).WithConcurrency(concurrency).WithScopeLocker(locker)

		worked, err := runner.RunOnce(context.Background(), nil)
		if err != nil {
			t.Fatalf("concurrency=%d: beklenmeyen hata: %v", concurrency, err)
		}
		if !worked {
			t.Fatalf("concurrency=%d: kapsam varken worked=false döndü", concurrency)
		}
		if len(locker.attempts) != len(store.scopes) {
			t.Fatalf("concurrency=%d: %d kapsam denendi, beklenen %d",
				concurrency, len(locker.attempts), len(store.scopes))
		}
		for scope, n := range locker.attempts {
			if n != 1 {
				t.Fatalf("concurrency=%d: %v kapsamı %d kez işlendi, beklenen 1",
					concurrency, scope.TenantID, n)
			}
		}
	}
}

// TestRunOnce_ConcurrencyActuallyOverlaps, eşzamanlılık ayarının gerçekten
// paralellik ürettiğini doğrular (kapsamlar aynı anda etkin olmalı).
func TestRunOnce_ConcurrencyActuallyOverlaps(t *testing.T) {
	store := &schedulerStore{scopes: makeScopes(12)}
	locker := newCountingLocker(true)
	runner := newTestRunner(store).WithConcurrency(4).WithScopeLocker(locker)

	if _, err := runner.RunOnce(context.Background(), nil); err != nil {
		t.Fatalf("beklenmeyen hata: %v", err)
	}
	if locker.maxActive < 2 {
		t.Fatalf("eşzamanlı etkin kapsam en fazla %d oldu; paralellik çalışmıyor", locker.maxActive)
	}
	if locker.maxActive > 4 {
		t.Fatalf("eşzamanlı etkin kapsam %d oldu; sınır 4 aşıldı", locker.maxActive)
	}
	if locker.releases != len(store.scopes) {
		t.Fatalf("%d kilit bırakıldı, beklenen %d — kilit sızıntısı",
			locker.releases, len(store.scopes))
	}
}

// TestRunOnce_SequentialWhenConcurrencyOne, varsayılan sıralı davranışın
// korunduğunu doğrular (tek örnekli kurulumlar için).
func TestRunOnce_SequentialWhenConcurrencyOne(t *testing.T) {
	store := &schedulerStore{scopes: makeScopes(6)}
	locker := newCountingLocker(true)
	runner := newTestRunner(store).WithConcurrency(1).WithScopeLocker(locker)

	if _, err := runner.RunOnce(context.Background(), nil); err != nil {
		t.Fatalf("beklenmeyen hata: %v", err)
	}
	if locker.maxActive != 1 {
		t.Fatalf("concurrency=1 iken en fazla %d kapsam etkin oldu, beklenen 1", locker.maxActive)
	}
}

// TestRunOnce_NoScopes_ReportsNoWork, iş yokken worked=false dönmesini doğrular
// (worker.RunLoop bunu bekleme sinyali olarak kullanır).
func TestRunOnce_NoScopes_ReportsNoWork(t *testing.T) {
	runner := newTestRunner(&schedulerStore{scopes: nil})
	worked, err := runner.RunOnce(context.Background(), nil)
	if err != nil {
		t.Fatalf("beklenmeyen hata: %v", err)
	}
	if worked {
		t.Fatal("kapsam yokken worked=true döndü")
	}
}

// TestRunOnce_WithoutLocker_StillRuns, kilit yapılandırılmamışsa (tek örnekli
// kurulum) kapsamların yine işlendiğini doğrular.
func TestRunOnce_WithoutLocker_StillRuns(t *testing.T) {
	store := &schedulerStore{scopes: makeScopes(3)}
	runner := newTestRunner(store).WithConcurrency(2)
	worked, err := runner.RunOnce(context.Background(), nil)
	if err != nil {
		t.Fatalf("beklenmeyen hata: %v", err)
	}
	if !worked {
		t.Fatal("kapsam varken worked=false döndü")
	}
}

// TestWithConcurrency_ClampsBelowOne, geçersiz ayarın sıralı çalışmaya
// düşürüldüğünü doğrular (0 verilirse hiçbir kapsam işlenmemesi olmamalı).
func TestWithConcurrency_ClampsBelowOne(t *testing.T) {
	store := &schedulerStore{scopes: makeScopes(4)}
	locker := newCountingLocker(false)
	runner := newTestRunner(store).WithConcurrency(0).WithScopeLocker(locker)

	if _, err := runner.RunOnce(context.Background(), nil); err != nil {
		t.Fatalf("beklenmeyen hata: %v", err)
	}
	if len(locker.attempts) != 4 {
		t.Fatalf("concurrency=0 iken %d kapsam işlendi, beklenen 4", len(locker.attempts))
	}
}

// panicLocker, kilit verildikten sonra işlemenin panic ettiği durumu taklit eder.
type panicLocker struct {
	mu       sync.Mutex
	releases int
}

func (l *panicLocker) TryLockScope(context.Context, domain.ListingSyncScope) (func(), bool, error) {
	return func() {
		l.mu.Lock()
		l.releases++
		l.mu.Unlock()
	}, true, nil
}

// panicStore, senkron sırasında panic üreterek tek mağazanın bozuk verisini taklit eder.
type panicStore struct {
	schedulerStore
}

func (s *panicStore) ListDirty(context.Context, uuid.UUID, string, time.Time, int) ([]*domain.ProductListing, error) {
	panic("bozuk mağaza verisi")
}

// TestRunOnce_ScopePanic_DoesNotKillOtherScopes, tek kapsamdaki panic'in ne
// süreci düşürdüğünü ne de kilidi sızdırdığını doğrular — kullanıcının açıkça
// istediği "mağazalar birbirini etkilemesin" güvencesi.
func TestRunOnce_ScopePanic_DoesNotKillOtherScopes(t *testing.T) {
	store := &panicStore{schedulerStore{scopes: makeScopes(5)}}
	locker := &panicLocker{}
	runner := newTestRunner(store).WithConcurrency(3).WithScopeLocker(locker)

	worked, err := runner.RunOnce(context.Background(), nil)
	if err != nil {
		t.Fatalf("panic yayıldı: %v", err)
	}
	if !worked {
		t.Fatal("kapsam varken worked=false döndü")
	}
	locker.mu.Lock()
	defer locker.mu.Unlock()
	if locker.releases != 5 {
		t.Fatalf("%d kilit bırakıldı, beklenen 5 — panic kilidi sızdırdı", locker.releases)
	}
}

// TestRunOnce_MarketplaceFilter, kanal başına çalışan worker'ın yalnızca kendi
// kanalının kapsamlarını aldığını doğrular. Filtre olmadan Trendyol worker'ı
// Shopify kapsamlarını da alır ve istemci çözemeden hata üretir.
func TestRunOnce_MarketplaceFilter(t *testing.T) {
	tenant := uuid.New()
	store := &schedulerStore{scopes: []domain.ListingSyncScope{
		{TenantID: tenant, MarketplaceCode: "TY"},
		{TenantID: tenant, MarketplaceCode: "shopify"},
		{TenantID: uuid.New(), MarketplaceCode: "hepsiburada"},
	}}

	locker := newCountingLocker(false)
	runner := newTestRunner(store).WithMarketplaces("TY").WithScopeLocker(locker)
	if _, err := runner.RunOnce(context.Background(), nil); err != nil {
		t.Fatalf("beklenmeyen hata: %v", err)
	}
	if len(locker.attempts) != 1 {
		t.Fatalf("%d kapsam işlendi, beklenen 1 (yalnızca TY)", len(locker.attempts))
	}
	for scope := range locker.attempts {
		if scope.MarketplaceCode != "TY" {
			t.Fatalf("filtre dışı kanal işlendi: %s", scope.MarketplaceCode)
		}
	}

	// Filtresiz worker tüm kanalları almalı (tek worker'lı kurulum).
	all := newCountingLocker(false)
	if _, err := newTestRunner(store).WithScopeLocker(all).RunOnce(context.Background(), nil); err != nil {
		t.Fatalf("beklenmeyen hata: %v", err)
	}
	if len(all.attempts) != 3 {
		t.Fatalf("filtresiz worker %d kapsam işledi, beklenen 3", len(all.attempts))
	}

	// Yalnızca başka kanalları olan bir worker iş bulmamalı.
	none := newCountingLocker(false)
	worked, err := newTestRunner(store).WithMarketplaces("n11").WithScopeLocker(none).RunOnce(context.Background(), nil)
	if err != nil {
		t.Fatalf("beklenmeyen hata: %v", err)
	}
	if worked || len(none.attempts) != 0 {
		t.Fatalf("eşleşen kanal yokken iş yapıldı (worked=%v, deneme=%d)", worked, len(none.attempts))
	}
}
