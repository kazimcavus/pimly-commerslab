package application

import (
	"context"
	"strings"
	"time"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/channels/domain"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// Handlers, Channels kullanım senaryolarını yürütür (.NET'teki ~20 handler'ın
// Go karşılığı tek yapıda toplanmıştır).
type Handlers struct {
	repo             ChannelsRepository
	catalog          CatalogGateway
	attributesClient CategoryAttributesClient
}

// NewHandlers, bağımlılıklarıyla handler'ları oluşturur.
func NewHandlers(repo ChannelsRepository, catalog CatalogGateway, attributesClient CategoryAttributesClient) *Handlers {
	return &Handlers{repo: repo, catalog: catalog, attributesClient: attributesClient}
}

// resolveMarketplace, kod parametresini pazaryerine çözer.
func resolveMarketplace(code string) sharedkernel.ResultOf[sharedkernel.Marketplace] {
	return sharedkernel.MarketplaceFromCode(code)
}

// ListMarketplaces, desteklenen pazaryerlerini bağlantı durumuyla döner.
func (h *Handlers) ListMarketplaces(ctx context.Context, tenantID uuid.UUID) sharedkernel.ResultOf[[]MarketplaceDto] {
	configured, err := h.repo.GetConfiguredMarketplaceCodes(ctx, tenantID)
	if err != nil {
		return sharedkernel.FailOf[[]MarketplaceDto](sharedkernel.NewInternalError(err.Error()))
	}
	_, isConfigured := configured[sharedkernel.MarketplaceCodeTrendyol]
	return sharedkernel.OkOf([]MarketplaceDto{{
		Code: sharedkernel.MarketplaceTrendyol.Code(), Name: sharedkernel.MarketplaceTrendyol.Name(),
		IsActive: true, IsConfigured: isConfigured,
	}})
}

// GetConnection, pazaryeri bağlantısını maskeli döner; yoksa not_found.
func (h *Handlers) GetConnection(ctx context.Context, tenantID uuid.UUID, code string) sharedkernel.ResultOf[MarketplaceConnectionDto] {
	marketplace := resolveMarketplace(code)
	if marketplace.IsFailure() {
		return sharedkernel.FailOf[MarketplaceConnectionDto](marketplace.Err())
	}
	connection, err := h.repo.GetConnection(ctx, tenantID, marketplace.Value().Code())
	if err != nil {
		return sharedkernel.FailOf[MarketplaceConnectionDto](sharedkernel.NewInternalError(err.Error()))
	}
	if connection == nil {
		return sharedkernel.FailOf[MarketplaceConnectionDto](sharedkernel.NewNotFoundError("Marketplace connection not found."))
	}
	return sharedkernel.OkOf(connectionToDto(connection))
}

// UpsertConnection, bağlantı kimlik bilgilerini oluşturur/günceller.
func (h *Handlers) UpsertConnection(ctx context.Context, tenantID uuid.UUID, code string, sellerID *string, apiKey string, apiSecret *string, isEnabled bool) sharedkernel.ResultOf[MarketplaceConnectionDto] {
	marketplace := resolveMarketplace(code)
	if marketplace.IsFailure() {
		return sharedkernel.FailOf[MarketplaceConnectionDto](marketplace.Err())
	}

	existing, err := h.repo.GetConnection(ctx, tenantID, marketplace.Value().Code())
	if err != nil {
		return sharedkernel.FailOf[MarketplaceConnectionDto](sharedkernel.NewInternalError(err.Error()))
	}
	if existing == nil {
		createResult := domain.NewMarketplaceConnection(marketplace.Value().Code(), sellerID, apiKey, apiSecret, isEnabled)
		if createResult.IsFailure() {
			return sharedkernel.FailOf[MarketplaceConnectionDto](createResult.Err())
		}
		if err := h.repo.AddConnection(ctx, tenantID, createResult.Value()); err != nil {
			return sharedkernel.FailOf[MarketplaceConnectionDto](sharedkernel.NewInternalError(err.Error()))
		}
		return sharedkernel.OkOf(connectionToDto(createResult.Value()))
	}

	if updateResult := existing.Update(sellerID, apiKey, apiSecret, isEnabled); updateResult.IsFailure() {
		return sharedkernel.FailOf[MarketplaceConnectionDto](updateResult.Err())
	}
	if err := h.repo.UpdateConnection(ctx, tenantID, existing); err != nil {
		return sharedkernel.FailOf[MarketplaceConnectionDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(connectionToDto(existing))
}

// GetTaxonomyStatus, taksonomi senkronunun özet durumunu döner.
func (h *Handlers) GetTaxonomyStatus(ctx context.Context, code string) sharedkernel.ResultOf[TaxonomyStatusDto] {
	marketplace := resolveMarketplace(code)
	if marketplace.IsFailure() {
		return sharedkernel.FailOf[TaxonomyStatusDto](marketplace.Err())
	}
	mp := marketplace.Value().Code()

	active, err := h.repo.GetActiveTaxonomyRun(ctx, mp)
	if err != nil {
		return sharedkernel.FailOf[TaxonomyStatusDto](sharedkernel.NewInternalError(err.Error()))
	}
	lastCompleted, err := h.repo.GetLatestCompletedTaxonomyRun(ctx, mp)
	if err != nil {
		return sharedkernel.FailOf[TaxonomyStatusDto](sharedkernel.NewInternalError(err.Error()))
	}
	cachedCount, err := h.repo.CountExternalCategories(ctx, mp)
	if err != nil {
		return sharedkernel.FailOf[TaxonomyStatusDto](sharedkernel.NewInternalError(err.Error()))
	}

	dto := TaxonomyStatusDto{
		MarketplaceCode: mp, IsSyncActive: active != nil, CachedCategoryCount: cachedCount,
	}
	if active != nil {
		dto.ActiveSyncRunID = &active.ID
	}
	if lastCompleted != nil {
		dto.LastCompletedAt = lastCompleted.CompletedAt
		completed := taxonomyRunToDto(lastCompleted)
		dto.LastCompletedRun = &completed
	}
	return sharedkernel.OkOf(dto)
}

// EnqueueTaxonomySync, yeni taksonomi senkron işi kuyruklar; aktif iş varsa
// conflict döner (worker, taxonomy_sync_runs tablosunu poll ederek işler).
func (h *Handlers) EnqueueTaxonomySync(ctx context.Context, code string) sharedkernel.ResultOf[TaxonomySyncRunDto] {
	marketplace := resolveMarketplace(code)
	if marketplace.IsFailure() {
		return sharedkernel.FailOf[TaxonomySyncRunDto](marketplace.Err())
	}
	mp := marketplace.Value().Code()

	active, err := h.repo.GetActiveTaxonomyRun(ctx, mp)
	if err != nil {
		return sharedkernel.FailOf[TaxonomySyncRunDto](sharedkernel.NewInternalError(err.Error()))
	}
	if active != nil {
		return sharedkernel.FailOf[TaxonomySyncRunDto](sharedkernel.NewConflictError(
			"A taxonomy sync is already pending or running for this marketplace."))
	}

	run := domain.NewTaxonomySyncRun(mp, time.Now().UTC())
	if err := h.repo.AddTaxonomyRun(ctx, run); err != nil {
		return sharedkernel.FailOf[TaxonomySyncRunDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(taxonomyRunToDto(run))
}

// GetTaxonomySyncRun, senkron işinin ayrıntısını döner.
func (h *Handlers) GetTaxonomySyncRun(ctx context.Context, code string, runID uuid.UUID) sharedkernel.ResultOf[TaxonomySyncRunDto] {
	marketplace := resolveMarketplace(code)
	if marketplace.IsFailure() {
		return sharedkernel.FailOf[TaxonomySyncRunDto](marketplace.Err())
	}
	run, err := h.repo.GetTaxonomyRun(ctx, runID)
	if err != nil {
		return sharedkernel.FailOf[TaxonomySyncRunDto](sharedkernel.NewInternalError(err.Error()))
	}
	if run == nil || run.MarketplaceCode != marketplace.Value().Code() {
		return sharedkernel.FailOf[TaxonomySyncRunDto](sharedkernel.NewNotFoundError("Taxonomy sync run not found."))
	}
	return sharedkernel.OkOf(taxonomyRunToDto(run))
}

// requireImportableConnection, import/yayın kuyruğu için bağlantı koşullarını
// doğrular: bağlantı var, etkin, satıcı kimliği ve gizli anahtar dolu.
func (h *Handlers) requireImportableConnection(ctx context.Context, tenantID uuid.UUID, marketplaceCode, missingMessage, purpose string) *sharedkernel.Error {
	connection, err := h.repo.GetConnection(ctx, tenantID, marketplaceCode)
	if err != nil {
		return sharedkernel.NewInternalError(err.Error())
	}
	if connection == nil {
		return sharedkernel.NewNotFoundError(missingMessage)
	}
	if !connection.IsEnabled {
		return sharedkernel.NewValidationError("Marketplace connection is disabled.")
	}
	if connection.SellerID == nil || strings.TrimSpace(*connection.SellerID) == "" ||
		connection.ApiSecret == nil || strings.TrimSpace(*connection.ApiSecret) == "" {
		return sharedkernel.NewValidationError(
			"Marketplace connection requires seller id and api secret for " + purpose + ".")
	}
	return nil
}

// EnqueueProductImport, yeni ürün import işi kuyruklar.
func (h *Handlers) EnqueueProductImport(ctx context.Context, tenantID uuid.UUID, code string) sharedkernel.ResultOf[ProductImportRunDto] {
	marketplace := resolveMarketplace(code)
	if marketplace.IsFailure() {
		return sharedkernel.FailOf[ProductImportRunDto](marketplace.Err())
	}
	mp := marketplace.Value().Code()

	if cerr := h.requireImportableConnection(ctx, tenantID, mp,
		"Marketplace connection is required before importing products.", "product import"); cerr != nil {
		return sharedkernel.FailOf[ProductImportRunDto](cerr)
	}

	active, err := h.repo.GetActiveImportRun(ctx, tenantID, mp)
	if err != nil {
		return sharedkernel.FailOf[ProductImportRunDto](sharedkernel.NewInternalError(err.Error()))
	}
	if active != nil {
		return sharedkernel.FailOf[ProductImportRunDto](sharedkernel.NewConflictError(
			"A product import is already pending or running for this marketplace."))
	}

	createResult := domain.NewProductImportRun(tenantID, mp, time.Now().UTC())
	if createResult.IsFailure() {
		return sharedkernel.FailOf[ProductImportRunDto](createResult.Err())
	}
	if err := h.repo.AddImportRun(ctx, createResult.Value()); err != nil {
		return sharedkernel.FailOf[ProductImportRunDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(importRunToDto(createResult.Value()))
}

// GetProductImportRun, import işinin ayrıntısını döner.
func (h *Handlers) GetProductImportRun(ctx context.Context, tenantID uuid.UUID, code string, runID uuid.UUID) sharedkernel.ResultOf[ProductImportRunDto] {
	marketplace := resolveMarketplace(code)
	if marketplace.IsFailure() {
		return sharedkernel.FailOf[ProductImportRunDto](marketplace.Err())
	}
	run, err := h.repo.GetImportRun(ctx, tenantID, runID)
	if err != nil {
		return sharedkernel.FailOf[ProductImportRunDto](sharedkernel.NewInternalError(err.Error()))
	}
	if run == nil || run.MarketplaceCode != marketplace.Value().Code() {
		return sharedkernel.FailOf[ProductImportRunDto](sharedkernel.NewNotFoundError("Product import run not found."))
	}
	return sharedkernel.OkOf(importRunToDto(run))
}

// ListProductImportRuns, son import işlerini yeniden eskiye döner.
func (h *Handlers) ListProductImportRuns(ctx context.Context, tenantID uuid.UUID, code string, limit int) sharedkernel.ResultOf[[]ProductImportRunSummaryDto] {
	marketplace := resolveMarketplace(code)
	if marketplace.IsFailure() {
		return sharedkernel.FailOf[[]ProductImportRunSummaryDto](marketplace.Err())
	}
	runs, err := h.repo.ListRecentImportRuns(ctx, tenantID, marketplace.Value().Code(), limit)
	if err != nil {
		return sharedkernel.FailOf[[]ProductImportRunSummaryDto](sharedkernel.NewInternalError(err.Error()))
	}
	dtos := make([]ProductImportRunSummaryDto, len(runs))
	for i, run := range runs {
		dtos[i] = importRunToSummaryDto(run)
	}
	return sharedkernel.OkOf(dtos)
}

// EnqueuePublication, yeni ürün yayın işi kuyruklar.
func (h *Handlers) EnqueuePublication(ctx context.Context, tenantID uuid.UUID, code string) sharedkernel.ResultOf[ProductPublicationRunDto] {
	marketplace := resolveMarketplace(code)
	if marketplace.IsFailure() {
		return sharedkernel.FailOf[ProductPublicationRunDto](marketplace.Err())
	}
	mp := marketplace.Value().Code()

	if cerr := h.requireImportableConnection(ctx, tenantID, mp,
		"Marketplace connection is required before publishing products.", "publishing"); cerr != nil {
		return sharedkernel.FailOf[ProductPublicationRunDto](cerr)
	}

	active, err := h.repo.GetActivePublicationRun(ctx, tenantID, mp)
	if err != nil {
		return sharedkernel.FailOf[ProductPublicationRunDto](sharedkernel.NewInternalError(err.Error()))
	}
	if active != nil {
		return sharedkernel.FailOf[ProductPublicationRunDto](sharedkernel.NewConflictError(
			"A publication is already pending or running for this marketplace."))
	}

	createResult := domain.NewProductPublicationRun(tenantID, mp, time.Now().UTC())
	if createResult.IsFailure() {
		return sharedkernel.FailOf[ProductPublicationRunDto](createResult.Err())
	}
	if err := h.repo.AddPublicationRun(ctx, createResult.Value()); err != nil {
		return sharedkernel.FailOf[ProductPublicationRunDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(publicationRunToDto(createResult.Value()))
}

// GetPublicationRun, yayın işinin ayrıntısını döner.
func (h *Handlers) GetPublicationRun(ctx context.Context, tenantID uuid.UUID, code string, runID uuid.UUID) sharedkernel.ResultOf[ProductPublicationRunDto] {
	marketplace := resolveMarketplace(code)
	if marketplace.IsFailure() {
		return sharedkernel.FailOf[ProductPublicationRunDto](marketplace.Err())
	}
	run, err := h.repo.GetPublicationRun(ctx, tenantID, runID)
	if err != nil {
		return sharedkernel.FailOf[ProductPublicationRunDto](sharedkernel.NewInternalError(err.Error()))
	}
	if run == nil || run.MarketplaceCode != marketplace.Value().Code() {
		return sharedkernel.FailOf[ProductPublicationRunDto](sharedkernel.NewNotFoundError("Publication run not found."))
	}
	return sharedkernel.OkOf(publicationRunToDto(run))
}

// SearchExternalCategories, cache'lenmiş harici kategorilerde ad/yol araması yapar.
func (h *Handlers) SearchExternalCategories(ctx context.Context, code string, query *string, limit int) sharedkernel.ResultOf[[]ExternalCategoryDto] {
	marketplace := resolveMarketplace(code)
	if marketplace.IsFailure() {
		return sharedkernel.FailOf[[]ExternalCategoryDto](marketplace.Err())
	}
	categories, err := h.repo.SearchExternalCategories(ctx, marketplace.Value().Code(), query, limit)
	if err != nil {
		return sharedkernel.FailOf[[]ExternalCategoryDto](sharedkernel.NewInternalError(err.Error()))
	}
	dtos := make([]ExternalCategoryDto, len(categories))
	for i, category := range categories {
		dtos[i] = externalCategoryToDto(category)
	}
	return sharedkernel.OkOf(dtos)
}

// resolveEnabledCredentials, pazaryeri-global uçlar için etkin herhangi bir
// bağlantının kimlik bilgilerini döner; yoksa nil (.NET
// ResolveAnyEnabledCredentialsAsync karşılığı — tenant seçimi önemsizdir).
func (h *Handlers) resolveEnabledCredentials(ctx context.Context, tenantID uuid.UUID, marketplaceCode string) (*MarketplaceCredentials, error) {
	connection, err := h.repo.GetConnection(ctx, tenantID, marketplaceCode)
	if err != nil || connection == nil || !connection.IsEnabled {
		return nil, err
	}
	return &MarketplaceCredentials{
		SellerID: connection.SellerID, ApiKey: connection.ApiKey, ApiSecret: connection.ApiSecret}, nil
}

// ListExternalCategoryAttributes, eşli kategorinin pazaryeri özellik şemasını
// döner; önce cache pazaryerinden tazelenir (ürün import hattı da aynı yolu kullanır).
func (h *Handlers) ListExternalCategoryAttributes(ctx context.Context, tenantID uuid.UUID, code string, catalogCategoryID uuid.UUID) sharedkernel.ResultOf[[]ExternalCategoryAttributeDto] {
	marketplace := resolveMarketplace(code)
	if marketplace.IsFailure() {
		return sharedkernel.FailOf[[]ExternalCategoryAttributeDto](marketplace.Err())
	}
	mp := marketplace.Value().Code()

	externalCategoryID, err := h.repo.ResolveExternalCategoryID(ctx, tenantID, mp, catalogCategoryID)
	if err != nil {
		return sharedkernel.FailOf[[]ExternalCategoryAttributeDto](sharedkernel.NewInternalError(err.Error()))
	}
	if externalCategoryID == nil {
		return sharedkernel.FailOf[[]ExternalCategoryAttributeDto](sharedkernel.NewNotFoundError(
			"Category channel mapping required before listing external attributes."))
	}

	credentials, err := h.resolveEnabledCredentials(ctx, tenantID, mp)
	if err != nil {
		return sharedkernel.FailOf[[]ExternalCategoryAttributeDto](sharedkernel.NewInternalError(err.Error()))
	}
	fetchResult := h.attributesClient.FetchCategoryAttributes(ctx, credentials, *externalCategoryID)
	if fetchResult.IsFailure() {
		return sharedkernel.FailOf[[]ExternalCategoryAttributeDto](fetchResult.Err())
	}
	if err := h.repo.RefreshExternalAttributes(ctx, mp, *externalCategoryID, fetchResult.Value(), time.Now().UTC()); err != nil {
		return sharedkernel.FailOf[[]ExternalCategoryAttributeDto](sharedkernel.NewInternalError(err.Error()))
	}

	attributes, err := h.repo.ListExternalAttributes(ctx, mp, *externalCategoryID)
	if err != nil {
		return sharedkernel.FailOf[[]ExternalCategoryAttributeDto](sharedkernel.NewInternalError(err.Error()))
	}
	values, err := h.repo.ListExternalValues(ctx, mp, *externalCategoryID)
	if err != nil {
		return sharedkernel.FailOf[[]ExternalCategoryAttributeDto](sharedkernel.NewInternalError(err.Error()))
	}

	dtos := make([]ExternalCategoryAttributeDto, len(attributes))
	for i, attribute := range attributes {
		var attributeValues []ExternalAttributeValueDto
		for _, value := range values {
			if value.ExternalAttributeID == attribute.ExternalAttributeID {
				attributeValues = append(attributeValues, ExternalAttributeValueDto{
					ExternalAttributeID: value.ExternalAttributeID, ExternalValueID: value.ExternalValueID,
					Name: value.Name, SyncedAt: value.SyncedAt,
				})
			}
		}
		if attributeValues == nil {
			attributeValues = []ExternalAttributeValueDto{}
		}
		dtos[i] = ExternalCategoryAttributeDto{
			ExternalCategoryID: attribute.ExternalCategoryID, ExternalAttributeID: attribute.ExternalAttributeID,
			Name: attribute.Name, Required: attribute.Required, AllowCustom: attribute.AllowCustom,
			IsVariant: attribute.IsVariant, SyncedAt: attribute.SyncedAt,
			Values: attributeValues, IsSlicer: attribute.IsSlicer,
		}
	}
	return sharedkernel.OkOf(dtos)
}
