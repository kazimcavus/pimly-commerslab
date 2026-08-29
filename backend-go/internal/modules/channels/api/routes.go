// Package api, Channels modülünün 23 HTTP ucunu /api/v1/channels öneki altında
// kaydeder (.NET Channels.Api.ChannelsEndpoints karşılığı). Tüm grup JWT
// zorunludur; tenant kimliği context'ten alınıp handler'lara açıkça geçirilir.
package api

import (
	"net/http"
	"strconv"

	"github.com/go-chi/chi/v5"
	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/channels/application"
	"pimly.commerslab/backend-go/internal/modules/channels/domain"
	"pimly.commerslab/backend-go/internal/platform/httpx"
	"pimly.commerslab/backend-go/internal/sharedkernel/tenancy"
)

// pathUUID, {param} yol değişkenini UUID olarak çözer; geçersizse gövdesiz 404.
func pathUUID(w http.ResponseWriter, r *http.Request, name string) (uuid.UUID, bool) {
	id, err := uuid.Parse(chi.URLParam(r, name))
	if err != nil {
		w.WriteHeader(http.StatusNotFound)
		return uuid.Nil, false
	}
	return id, true
}

// queryIntOr, tamsayı sorgu parametresini okur; yoksa varsayılanı döner.
func queryIntOr(r *http.Request, name string, fallback int) int {
	if raw := r.URL.Query().Get(name); raw != "" {
		if value, err := strconv.Atoi(raw); err == nil {
			return value
		}
	}
	return fallback
}

// Mount, Channels rotalarını kaydeder.
func Mount(r chi.Router, h *application.Handlers, authMiddleware func(http.Handler) http.Handler) {
	r.Route("/api/v1/channels", func(g chi.Router) {
		g.Use(authMiddleware)

		g.Get("/marketplaces", func(w http.ResponseWriter, r *http.Request) {
			httpx.WriteOK(w, r, h.ListMarketplaces(r.Context(), tenancy.MustFromContext(r.Context())))
		})

		g.Get("/marketplaces/{code}/connection", func(w http.ResponseWriter, r *http.Request) {
			httpx.WriteOK(w, r, h.GetConnection(r.Context(), tenancy.MustFromContext(r.Context()),
				chi.URLParam(r, "code")))
		})

		g.Put("/marketplaces/{code}/connection", func(w http.ResponseWriter, r *http.Request) {
			// settings alanları gönderilmezse (nil) mevcut ayarlar korunur;
			// yeni bağlantıda varsayılanlar uygulanır.
			type settingsRequest struct {
				DisplayName        *string                `json:"display_name"`
				ExternalLocationID *string                `json:"external_location_id"`
				PricesIncludeVat   *bool                  `json:"prices_include_vat"`
				ExclusionRules     *domain.ExclusionRules `json:"exclusion_rules"`
			}
			type upsertRequest struct {
				SellerID  *string          `json:"seller_id"`
				ApiKey    string           `json:"api_key"`
				ApiSecret *string          `json:"api_secret"`
				IsEnabled bool             `json:"is_enabled"`
				Settings  *settingsRequest `json:"settings"`
			}
			body, derr := httpx.DecodeJSON[upsertRequest](r)
			if derr != nil {
				httpx.WriteProblem(w, r, derr)
				return
			}
			var settings *domain.ConnectionSettings
			if body.Settings != nil {
				resolved := domain.DefaultConnectionSettings()
				resolved.DisplayName = body.Settings.DisplayName
				resolved.ExternalLocationID = body.Settings.ExternalLocationID
				if body.Settings.PricesIncludeVat != nil {
					resolved.PricesIncludeVat = *body.Settings.PricesIncludeVat
				}
				if body.Settings.ExclusionRules != nil {
					resolved.ExclusionRules = *body.Settings.ExclusionRules
				}
				settings = &resolved
			}
			httpx.WriteOK(w, r, h.UpsertConnection(r.Context(), tenancy.MustFromContext(r.Context()),
				chi.URLParam(r, "code"), body.SellerID, body.ApiKey, body.ApiSecret, body.IsEnabled, settings))
		})

		g.Get("/marketplaces/{code}/taxonomy/status", func(w http.ResponseWriter, r *http.Request) {
			httpx.WriteOK(w, r, h.GetTaxonomyStatus(r.Context(), chi.URLParam(r, "code")))
		})

		g.Get("/marketplaces/{code}/taxonomy/sync-runs/{syncRunId}", func(w http.ResponseWriter, r *http.Request) {
			runID, ok := pathUUID(w, r, "syncRunId")
			if !ok {
				return
			}
			httpx.WriteOK(w, r, h.GetTaxonomySyncRun(r.Context(), chi.URLParam(r, "code"), runID))
		})

		g.Post("/marketplaces/{code}/taxonomy/sync-runs", func(w http.ResponseWriter, r *http.Request) {
			code := chi.URLParam(r, "code")
			result := h.EnqueueTaxonomySync(r.Context(), code)
			httpx.WriteAccepted(w, r, result,
				"/api/v1/channels/marketplaces/"+code+"/taxonomy/sync-runs/"+acceptedID(result.IsSuccess(), func() string {
					return result.Value().ID.String()
				}))
		})

		g.Get("/products/{productId}/readiness", func(w http.ResponseWriter, r *http.Request) {
			productID, ok := pathUUID(w, r, "productId")
			if !ok {
				return
			}
			httpx.WriteOK(w, r, h.GetProductReadiness(r.Context(), tenancy.MustFromContext(r.Context()), productID))
		})

		g.Post("/marketplaces/{code}/imports", func(w http.ResponseWriter, r *http.Request) {
			code := chi.URLParam(r, "code")
			result := h.EnqueueProductImport(r.Context(), tenancy.MustFromContext(r.Context()), code)
			httpx.WriteAccepted(w, r, result,
				"/api/v1/channels/marketplaces/"+code+"/imports/"+acceptedID(result.IsSuccess(), func() string {
					return result.Value().ID.String()
				}))
		})

		g.Get("/marketplaces/{code}/imports/{runId}", func(w http.ResponseWriter, r *http.Request) {
			runID, ok := pathUUID(w, r, "runId")
			if !ok {
				return
			}
			httpx.WriteOK(w, r, h.GetProductImportRun(r.Context(), tenancy.MustFromContext(r.Context()),
				chi.URLParam(r, "code"), runID))
		})

		g.Get("/marketplaces/{code}/imports", func(w http.ResponseWriter, r *http.Request) {
			httpx.WriteOK(w, r, h.ListProductImportRuns(r.Context(), tenancy.MustFromContext(r.Context()),
				chi.URLParam(r, "code"), queryIntOr(r, "limit", 20)))
		})

		g.Post("/marketplaces/{code}/publications", func(w http.ResponseWriter, r *http.Request) {
			code := chi.URLParam(r, "code")
			result := h.EnqueuePublication(r.Context(), tenancy.MustFromContext(r.Context()), code)
			httpx.WriteAccepted(w, r, result,
				"/api/v1/channels/marketplaces/"+code+"/publications/"+acceptedID(result.IsSuccess(), func() string {
					return result.Value().ID.String()
				}))
		})

		g.Get("/marketplaces/{code}/publications/{runId}", func(w http.ResponseWriter, r *http.Request) {
			runID, ok := pathUUID(w, r, "runId")
			if !ok {
				return
			}
			httpx.WriteOK(w, r, h.GetPublicationRun(r.Context(), tenancy.MustFromContext(r.Context()),
				chi.URLParam(r, "code"), runID))
		})

		g.Get("/marketplaces/{code}/categories", func(w http.ResponseWriter, r *http.Request) {
			var query *string
			if q := r.URL.Query().Get("q"); q != "" {
				query = &q
			}
			httpx.WriteOK(w, r, h.SearchExternalCategories(r.Context(), chi.URLParam(r, "code"),
				query, queryIntOr(r, "limit", 25)))
		})

		// Kategori eşlemeleri.
		g.Put("/marketplaces/{code}/category-mappings/{catalogCategoryId}", func(w http.ResponseWriter, r *http.Request) {
			categoryID, ok := pathUUID(w, r, "catalogCategoryId")
			if !ok {
				return
			}
			type upsertRequest struct {
				ExternalID string `json:"external_id"`
			}
			body, derr := httpx.DecodeJSON[upsertRequest](r)
			if derr != nil {
				httpx.WriteProblem(w, r, derr)
				return
			}
			httpx.WriteOK(w, r, h.UpsertCategoryMapping(r.Context(), tenancy.MustFromContext(r.Context()),
				chi.URLParam(r, "code"), categoryID, body.ExternalID))
		})

		g.Get("/marketplaces/{code}/category-mappings/{catalogCategoryId}", func(w http.ResponseWriter, r *http.Request) {
			categoryID, ok := pathUUID(w, r, "catalogCategoryId")
			if !ok {
				return
			}
			httpx.WriteOK(w, r, h.GetCategoryMapping(r.Context(), tenancy.MustFromContext(r.Context()),
				chi.URLParam(r, "code"), categoryID))
		})

		g.Get("/marketplaces/{code}/category-mappings", func(w http.ResponseWriter, r *http.Request) {
			var catalogCategoryID *uuid.UUID
			if raw := r.URL.Query().Get("catalog_category_id"); raw != "" {
				if id, err := uuid.Parse(raw); err == nil {
					catalogCategoryID = &id
				}
			}
			pr := httpx.QueryPagination(r)
			if pr.IsFailure() {
				httpx.WriteProblem(w, r, pr.Err())
				return
			}
			httpx.WriteOK(w, r, h.ListCategoryMappings(r.Context(), tenancy.MustFromContext(r.Context()),
				chi.URLParam(r, "code"), catalogCategoryID, pr.Value().Page, pr.Value().PageSize))
		})

		g.Delete("/marketplaces/{code}/category-mappings/{catalogCategoryId}", func(w http.ResponseWriter, r *http.Request) {
			categoryID, ok := pathUUID(w, r, "catalogCategoryId")
			if !ok {
				return
			}
			httpx.WriteResult(w, r, h.DeleteCategoryMapping(r.Context(), tenancy.MustFromContext(r.Context()),
				chi.URLParam(r, "code"), categoryID))
		})

		g.Get("/marketplaces/{code}/category-mappings/{catalogCategoryId}/external-attributes", func(w http.ResponseWriter, r *http.Request) {
			categoryID, ok := pathUUID(w, r, "catalogCategoryId")
			if !ok {
				return
			}
			httpx.WriteOK(w, r, h.ListExternalCategoryAttributes(r.Context(), tenancy.MustFromContext(r.Context()),
				chi.URLParam(r, "code"), categoryID))
		})

		// Alan eşlemeleri.
		g.Put("/marketplaces/{code}/category-mappings/{catalogCategoryId}/attribute-mappings", func(w http.ResponseWriter, r *http.Request) {
			categoryID, ok := pathUUID(w, r, "catalogCategoryId")
			if !ok {
				return
			}
			type upsertRequest struct {
				SourceType          string    `json:"source_type"`
				CatalogSourceID     uuid.UUID `json:"catalog_source_id"`
				ExternalAttributeID string    `json:"external_attribute_id"`
			}
			body, derr := httpx.DecodeJSON[upsertRequest](r)
			if derr != nil {
				httpx.WriteProblem(w, r, derr)
				return
			}
			httpx.WriteOK(w, r, h.UpsertAttributeMapping(r.Context(), tenancy.MustFromContext(r.Context()),
				chi.URLParam(r, "code"), categoryID, body.SourceType, body.CatalogSourceID, body.ExternalAttributeID))
		})

		g.Get("/marketplaces/{code}/category-mappings/{catalogCategoryId}/attribute-mappings", func(w http.ResponseWriter, r *http.Request) {
			categoryID, ok := pathUUID(w, r, "catalogCategoryId")
			if !ok {
				return
			}
			var sourceType *string
			if raw := r.URL.Query().Get("source_type"); raw != "" {
				sourceType = &raw
			}
			pr := httpx.QueryPagination(r)
			if pr.IsFailure() {
				httpx.WriteProblem(w, r, pr.Err())
				return
			}
			httpx.WriteOK(w, r, h.ListAttributeMappings(r.Context(), tenancy.MustFromContext(r.Context()),
				chi.URLParam(r, "code"), categoryID, sourceType, pr.Value().Page, pr.Value().PageSize))
		})

		g.Get("/marketplaces/{code}/category-mappings/{catalogCategoryId}/attribute-mappings/{mappingId}", func(w http.ResponseWriter, r *http.Request) {
			categoryID, ok := pathUUID(w, r, "catalogCategoryId")
			if !ok {
				return
			}
			mappingID, ok := pathUUID(w, r, "mappingId")
			if !ok {
				return
			}
			httpx.WriteOK(w, r, h.GetAttributeMapping(r.Context(), tenancy.MustFromContext(r.Context()),
				chi.URLParam(r, "code"), categoryID, mappingID))
		})

		g.Delete("/marketplaces/{code}/category-mappings/{catalogCategoryId}/attribute-mappings/{mappingId}", func(w http.ResponseWriter, r *http.Request) {
			categoryID, ok := pathUUID(w, r, "catalogCategoryId")
			if !ok {
				return
			}
			mappingID, ok := pathUUID(w, r, "mappingId")
			if !ok {
				return
			}
			httpx.WriteResult(w, r, h.DeleteAttributeMapping(r.Context(), tenancy.MustFromContext(r.Context()),
				chi.URLParam(r, "code"), categoryID, mappingID))
		})

		// Değer eşlemeleri.
		g.Put("/marketplaces/{code}/category-mappings/{catalogCategoryId}/attribute-mappings/{mappingId}/value-mappings", func(w http.ResponseWriter, r *http.Request) {
			categoryID, ok := pathUUID(w, r, "catalogCategoryId")
			if !ok {
				return
			}
			mappingID, ok := pathUUID(w, r, "mappingId")
			if !ok {
				return
			}
			type upsertRequest struct {
				Values []application.ValueMappingEntry `json:"values"`
			}
			body, derr := httpx.DecodeJSON[upsertRequest](r)
			if derr != nil {
				httpx.WriteProblem(w, r, derr)
				return
			}
			httpx.WriteOK(w, r, h.UpsertValueMappings(r.Context(), tenancy.MustFromContext(r.Context()),
				chi.URLParam(r, "code"), categoryID, mappingID, body.Values))
		})

		g.Get("/marketplaces/{code}/category-mappings/{catalogCategoryId}/attribute-mappings/{mappingId}/value-mappings", func(w http.ResponseWriter, r *http.Request) {
			categoryID, ok := pathUUID(w, r, "catalogCategoryId")
			if !ok {
				return
			}
			mappingID, ok := pathUUID(w, r, "mappingId")
			if !ok {
				return
			}
			httpx.WriteOK(w, r, h.ListValueMappings(r.Context(), tenancy.MustFromContext(r.Context()),
				chi.URLParam(r, "code"), categoryID, mappingID))
		})
	})
}

// acceptedID, 202 Location başlığı için sonuç kimliğini üretir; başarısız
// sonuçta Location zaten yazılmaz, yer tutucu döner.
func acceptedID(ok bool, id func() string) string {
	if !ok {
		return ""
	}
	return id()
}
