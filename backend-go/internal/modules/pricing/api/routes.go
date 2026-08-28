// Package api, Pricing modülünün HTTP uçlarını /api/v1/pricing öneki altında
// kaydeder (.NET Pricing.Api karşılığı): fiyat tanımları CRUD'u, kalem
// fiyatları, temel fiyat ve kanal fiyatları.
package api

import (
	"net/http"

	"github.com/go-chi/chi/v5"
	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/pricing/application"
	"pimly.commerslab/backend-go/internal/platform/httpx"
	"pimly.commerslab/backend-go/internal/sharedkernel/tenancy"
)

// definitionRequest, tanım oluşturma/güncelleme isteklerinin kablo biçimidir.
type definitionRequest struct {
	Name string  `json:"name"`
	Code *string `json:"code"`
}

// priceRequest, tutar taşıyan isteklerin ortak kablo biçimidir.
type priceRequest struct {
	Amount          application.Decimal  `json:"amount"`
	CompareAtAmount *application.Decimal `json:"compare_at_amount"`
	Currency        *string              `json:"currency"`
}

// pathID, {param} yol değişkenini UUID olarak çözer; geçersizse gövdesiz 404.
func pathID(w http.ResponseWriter, r *http.Request, name string) (uuid.UUID, bool) {
	id, err := uuid.Parse(chi.URLParam(r, name))
	if err != nil {
		w.WriteHeader(http.StatusNotFound)
		return uuid.Nil, false
	}
	return id, true
}

// Mount, Pricing rotalarını kaydeder; tüm grup JWT zorunludur.
func Mount(r chi.Router, h *application.PricingHandlers, authMiddleware func(http.Handler) http.Handler) {
	r.Route("/api/v1/pricing", func(g chi.Router) {
		g.Use(authMiddleware)

		// Fiyat tanımları (.NET PriceDefinitionEndpoints).
		g.Post("/price-definitions", func(w http.ResponseWriter, r *http.Request) {
			body, derr := httpx.DecodeJSON[definitionRequest](r)
			if derr != nil {
				httpx.WriteProblem(w, r, derr)
				return
			}
			result := h.CreateDefinition(r.Context(), tenancy.MustFromContext(r.Context()), body.Name, body.Code)
			httpx.WriteCreated(w, r, result, func(dto application.PriceDefinitionDto) string {
				return "/api/v1/pricing/price-definitions/" + dto.ID.String()
			})
		})

		g.Get("/price-definitions", func(w http.ResponseWriter, r *http.Request) {
			pr := httpx.QueryPagination(r)
			if pr.IsFailure() {
				httpx.WriteProblem(w, r, pr.Err())
				return
			}
			httpx.WriteOK(w, r, h.ListDefinitions(r.Context(), tenancy.MustFromContext(r.Context()),
				pr.Value().Page, pr.Value().PageSize))
		})

		g.Get("/price-definitions/{id}", func(w http.ResponseWriter, r *http.Request) {
			id, ok := pathID(w, r, "id")
			if !ok {
				return
			}
			httpx.WriteOK(w, r, h.GetDefinition(r.Context(), tenancy.MustFromContext(r.Context()), id))
		})

		g.Patch("/price-definitions/{id}", func(w http.ResponseWriter, r *http.Request) {
			id, ok := pathID(w, r, "id")
			if !ok {
				return
			}
			body, derr := httpx.DecodeJSON[definitionRequest](r)
			if derr != nil {
				httpx.WriteProblem(w, r, derr)
				return
			}
			httpx.WriteOK(w, r, h.UpdateDefinition(r.Context(), tenancy.MustFromContext(r.Context()),
				id, body.Name, body.Code))
		})

		g.Delete("/price-definitions/{id}", func(w http.ResponseWriter, r *http.Request) {
			id, ok := pathID(w, r, "id")
			if !ok {
				return
			}
			httpx.WriteResult(w, r, h.DeleteDefinition(r.Context(), tenancy.MustFromContext(r.Context()), id))
		})

		// Kalem fiyatları (.NET ItemPriceEndpoints).
		g.Get("/items/{itemId}/prices", func(w http.ResponseWriter, r *http.Request) {
			itemID, ok := pathID(w, r, "itemId")
			if !ok {
				return
			}
			httpx.WriteOK(w, r, h.ListItemPrices(r.Context(), tenancy.MustFromContext(r.Context()), itemID))
		})

		g.Put("/items/{itemId}/prices/{definitionId}", func(w http.ResponseWriter, r *http.Request) {
			itemID, ok := pathID(w, r, "itemId")
			if !ok {
				return
			}
			definitionID, ok := pathID(w, r, "definitionId")
			if !ok {
				return
			}
			body, derr := httpx.DecodeJSON[priceRequest](r)
			if derr != nil {
				httpx.WriteProblem(w, r, derr)
				return
			}
			httpx.WriteOK(w, r, h.UpsertItemPrice(r.Context(), tenancy.MustFromContext(r.Context()),
				itemID, definitionID, body.Amount, body.Currency))
		})

		g.Delete("/items/{itemId}/prices/{definitionId}", func(w http.ResponseWriter, r *http.Request) {
			itemID, ok := pathID(w, r, "itemId")
			if !ok {
				return
			}
			definitionID, ok := pathID(w, r, "definitionId")
			if !ok {
				return
			}
			httpx.WriteResult(w, r, h.DeleteItemPrice(r.Context(), tenancy.MustFromContext(r.Context()),
				itemID, definitionID))
		})

		// Temel fiyat (.NET BasePriceEndpoints).
		g.Get("/items/{itemId}/base-price", func(w http.ResponseWriter, r *http.Request) {
			itemID, ok := pathID(w, r, "itemId")
			if !ok {
				return
			}
			httpx.WriteOK(w, r, h.GetBasePrice(r.Context(), tenancy.MustFromContext(r.Context()), itemID))
		})

		g.Put("/items/{itemId}/base-price", func(w http.ResponseWriter, r *http.Request) {
			itemID, ok := pathID(w, r, "itemId")
			if !ok {
				return
			}
			body, derr := httpx.DecodeJSON[priceRequest](r)
			if derr != nil {
				httpx.WriteProblem(w, r, derr)
				return
			}
			httpx.WriteOK(w, r, h.SetBasePrice(r.Context(), tenancy.MustFromContext(r.Context()),
				itemID, body.Amount, body.CompareAtAmount, body.Currency))
		})

		// Kanal fiyatları (.NET ChannelPriceEndpoints).
		g.Get("/items/{itemId}/channel-prices", func(w http.ResponseWriter, r *http.Request) {
			itemID, ok := pathID(w, r, "itemId")
			if !ok {
				return
			}
			httpx.WriteOK(w, r, h.ListChannelPrices(r.Context(), tenancy.MustFromContext(r.Context()), itemID))
		})

		g.Get("/items/{itemId}/channel-prices/{marketplace}", func(w http.ResponseWriter, r *http.Request) {
			itemID, ok := pathID(w, r, "itemId")
			if !ok {
				return
			}
			httpx.WriteOK(w, r, h.GetChannelPrice(r.Context(), tenancy.MustFromContext(r.Context()),
				itemID, chi.URLParam(r, "marketplace")))
		})

		g.Put("/items/{itemId}/channel-prices/{marketplace}", func(w http.ResponseWriter, r *http.Request) {
			itemID, ok := pathID(w, r, "itemId")
			if !ok {
				return
			}
			body, derr := httpx.DecodeJSON[priceRequest](r)
			if derr != nil {
				httpx.WriteProblem(w, r, derr)
				return
			}
			httpx.WriteOK(w, r, h.SetChannelPrice(r.Context(), tenancy.MustFromContext(r.Context()),
				itemID, chi.URLParam(r, "marketplace"), body.Amount, body.CompareAtAmount, body.Currency))
		})
	})
}
