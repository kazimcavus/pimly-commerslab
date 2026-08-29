package publications

// Processor birim testleri (.NET ProcessPublicationHandler davranışlarının
// doğrulanması): eşlenmiş kategori yokken run hatasız tamamlanır, mevcut
// listelemelere dokunulmaz, içerik senkron hatası run'ı failed yapar, başarı
// yolunda run completed olur.

import (
	"context"
	"testing"
	"time"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/channels/application/listingsync"
	"pimly.commerslab/backend-go/internal/modules/channels/domain"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

type mockStore struct {
	connection       *domain.MarketplaceConnection
	categoryIDs      []uuid.UUID
	updatedRuns      []*domain.ProductPublicationRun
	listMappedErr    error
	getConnectionErr error
}

func (m *mockStore) UpdatePublicationRun(_ context.Context, run *domain.ProductPublicationRun) error {
	m.updatedRuns = append(m.updatedRuns, run)
	return nil
}
func (m *mockStore) GetConnection(context.Context, uuid.UUID, string) (*domain.MarketplaceConnection, error) {
	return m.connection, m.getConnectionErr
}
func (m *mockStore) ListMappedCategoryIDs(context.Context, uuid.UUID, string) ([]uuid.UUID, error) {
	return m.categoryIDs, m.listMappedErr
}

type mockItems struct {
	itemIDs map[uuid.UUID][]uuid.UUID // categoryID -> item ids
}

func (m *mockItems) ListItemIDsByCategories(_ context.Context, _ uuid.UUID, categoryIDs []uuid.UUID) ([]uuid.UUID, error) {
	ids := []uuid.UUID{}
	for _, categoryID := range categoryIDs {
		ids = append(ids, m.itemIDs[categoryID]...)
	}
	return ids, nil
}

type mockListings struct {
	existing []*domain.ProductListing
	added    []*domain.ProductListing
}

func (m *mockListings) ListByProductItems(context.Context, uuid.UUID, string, []uuid.UUID) ([]*domain.ProductListing, error) {
	return m.existing, nil
}
func (m *mockListings) AddRange(_ context.Context, listings []*domain.ProductListing) error {
	m.added = append(m.added, listings...)
	return nil
}

type mockContentSyncer struct {
	result sharedkernel.ResultOf[listingsync.ContentSyncSummary]
	called bool
}

func (m *mockContentSyncer) Sync(context.Context, uuid.UUID, string) sharedkernel.ResultOf[listingsync.ContentSyncSummary] {
	m.called = true
	return m.result
}

func enabledConnection() *domain.MarketplaceConnection {
	sellerID := "123"
	secret := "secret"
	return &domain.MarketplaceConnection{
		MarketplaceCode: "TY", SellerID: &sellerID, ApiKey: "key", ApiSecret: &secret, IsEnabled: true,
	}
}

func runningRun(tenantID uuid.UUID) *domain.ProductPublicationRun {
	createResult := domain.NewProductPublicationRun(tenantID, "TY", time.Now().UTC())
	run := createResult.Value()
	_ = run.MarkRunning(time.Now().UTC())
	return run
}

func TestProcess_NoMappedCategories_AddsErrorAndCompletes(t *testing.T) {
	tenantID := uuid.New()
	store := &mockStore{connection: enabledConnection(), categoryIDs: nil}
	items := &mockItems{itemIDs: map[uuid.UUID][]uuid.UUID{}}
	listings := &mockListings{}
	syncer := &mockContentSyncer{result: sharedkernel.OkOf(listingsync.ContentSyncSummary{})}

	p := NewProcessor(store, items, listings, syncer)
	run := runningRun(tenantID)
	if err := p.Process(context.Background(), run); err != nil {
		t.Fatalf("beklenmeyen hata: %v", err)
	}
	if run.Status != domain.PublicationCompleted {
		t.Fatalf("completed bekleniyordu: %v", run.Status)
	}
	if len(run.Errors) != 1 {
		t.Fatalf("kategori eşlemesi yokken bir hata kaydı bekleniyordu: %+v", run.Errors)
	}
	if run.Errors[0].ProductItemID != uuid.Nil {
		t.Fatalf("hata kaydı Guid.Empty karşılığı ile açılmalıydı: %v", run.Errors[0].ProductItemID)
	}
}

func TestProcess_EnrollsOnlyMissingListings(t *testing.T) {
	tenantID := uuid.New()
	categoryID := uuid.New()
	existingItemID := uuid.New()
	newItemID := uuid.New()

	store := &mockStore{connection: enabledConnection(), categoryIDs: []uuid.UUID{categoryID}}
	items := &mockItems{itemIDs: map[uuid.UUID][]uuid.UUID{categoryID: {existingItemID, newItemID}}}
	listings := &mockListings{existing: []*domain.ProductListing{{ProductItemID: existingItemID}}}
	syncer := &mockContentSyncer{result: sharedkernel.OkOf(listingsync.ContentSyncSummary{Examined: 1, Created: 1})}

	p := NewProcessor(store, items, listings, syncer)
	run := runningRun(tenantID)
	if err := p.Process(context.Background(), run); err != nil {
		t.Fatalf("beklenmeyen hata: %v", err)
	}
	if len(listings.added) != 1 || listings.added[0].ProductItemID != newItemID {
		t.Fatalf("yalnızca eksik kalem için listeleme açılmalıydı: %+v", listings.added)
	}
	if listings.added[0].Status != domain.ListingPending {
		t.Fatalf("yeni listeleme pending başlamalıydı: %v", listings.added[0].Status)
	}
	if !syncer.called {
		t.Fatal("içerik senkronu çağrılmalıydı")
	}
	if run.Status != domain.PublicationCompleted {
		t.Fatalf("completed bekleniyordu: %v", run.Status)
	}
	if run.PublishedItems != 1 {
		t.Fatalf("published=1 bekleniyordu: %d", run.PublishedItems)
	}
}

func TestProcess_ContentSyncFailure_MarksRunFailed(t *testing.T) {
	tenantID := uuid.New()
	categoryID := uuid.New()
	itemID := uuid.New()

	store := &mockStore{connection: enabledConnection(), categoryIDs: []uuid.UUID{categoryID}}
	items := &mockItems{itemIDs: map[uuid.UUID][]uuid.UUID{categoryID: {itemID}}}
	listings := &mockListings{}
	syncer := &mockContentSyncer{
		result: sharedkernel.FailOf[listingsync.ContentSyncSummary](sharedkernel.NewInternalError("boom")),
	}

	p := NewProcessor(store, items, listings, syncer)
	run := runningRun(tenantID)
	if err := p.Process(context.Background(), run); err != nil {
		t.Fatalf("işlem hatası worker seviyesine sızmamalıydı: %v", err)
	}
	if run.Status != domain.PublicationFailed {
		t.Fatalf("failed bekleniyordu: %v", run.Status)
	}
	if run.ErrorMessage == nil || *run.ErrorMessage != "boom" {
		t.Fatalf("hata mesajı korunmalıydı: %v", run.ErrorMessage)
	}
}

func TestProcess_MissingConnection_MarksRunFailed(t *testing.T) {
	store := &mockStore{connection: nil}
	items := &mockItems{}
	listings := &mockListings{}
	syncer := &mockContentSyncer{}

	p := NewProcessor(store, items, listings, syncer)
	run := runningRun(uuid.New())
	if err := p.Process(context.Background(), run); err != nil {
		t.Fatalf("beklenmeyen hata: %v", err)
	}
	if run.Status != domain.PublicationFailed {
		t.Fatalf("failed bekleniyordu: %v", run.Status)
	}
	if syncer.called {
		t.Fatal("bağlantı yokken içerik senkronu hiç çağrılmamalıydı")
	}
}

func TestProcess_NotRunning_ReturnsError(t *testing.T) {
	store := &mockStore{}
	items := &mockItems{}
	listings := &mockListings{}
	syncer := &mockContentSyncer{}

	p := NewProcessor(store, items, listings, syncer)
	createResult := domain.NewProductPublicationRun(uuid.New(), "TY", time.Now().UTC())
	run := createResult.Value() // hâlâ pending, running değil
	if err := p.Process(context.Background(), run); err == nil {
		t.Fatal("pending durumdaki iş işlenmemeliydi")
	}
}
