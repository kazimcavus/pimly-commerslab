// Package api, Inventory modülünün HTTP uçlarını /api/v1/inventory öneki
// altında kaydeder (.NET Inventory.Api karşılığı):
//
//	GET /items/{itemId}/stock — kalem stok seviyesi (kayıt yoksa 404)
//	PUT /items/{itemId}/stock — miktarı oluştur/güncelle
package api

import (
	"net/http"

	"github.com/go-chi/chi/v5"
	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/inventory/application"
	"pimly.commerslab/backend-go/internal/platform/httpx"
	"pimly.commerslab/backend-go/internal/sharedkernel/tenancy"
)

// Mount, Inventory rotalarını kaydeder; tüm grup JWT zorunludur.
func Mount(r chi.Router, h *application.StockHandlers, authMiddleware func(http.Handler) http.Handler) {
	r.Route("/api/v1/inventory", func(g chi.Router) {
		g.Use(authMiddleware)

		g.Get("/items/{itemId}/stock", func(w http.ResponseWriter, r *http.Request) {
			itemID, err := uuid.Parse(chi.URLParam(r, "itemId"))
			if err != nil {
				w.WriteHeader(http.StatusNotFound)
				return
			}
			httpx.WriteOK(w, r, h.Get(r.Context(), tenancy.MustFromContext(r.Context()), itemID))
		})

		g.Put("/items/{itemId}/stock", func(w http.ResponseWriter, r *http.Request) {
			itemID, err := uuid.Parse(chi.URLParam(r, "itemId"))
			if err != nil {
				w.WriteHeader(http.StatusNotFound)
				return
			}
			type setStockRequest struct {
				Quantity int `json:"quantity"`
			}
			body, derr := httpx.DecodeJSON[setStockRequest](r)
			if derr != nil {
				httpx.WriteProblem(w, r, derr)
				return
			}
			httpx.WriteOK(w, r, h.Set(r.Context(), tenancy.MustFromContext(r.Context()), itemID, body.Quantity))
		})
	})
}
