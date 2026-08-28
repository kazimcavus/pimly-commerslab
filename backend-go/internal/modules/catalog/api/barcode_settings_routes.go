package api

import (
	"net/http"

	"github.com/go-chi/chi/v5"

	"pimly.commerslab/backend-go/internal/modules/catalog/application"
	"pimly.commerslab/backend-go/internal/platform/httpx"
	"pimly.commerslab/backend-go/internal/sharedkernel/tenancy"
)

// mountBarcodeRoutes, barkod serisi uçlarını kaydeder (.NET BarcodeEndpoints
// karşılığı): GET/PUT /barcode-sequence, POST /barcodes:allocate,
// GET /barcode-allocations.
func mountBarcodeRoutes(g chi.Router, h *application.BarcodeHandlers) {
	g.Get("/barcode-sequence", func(w http.ResponseWriter, r *http.Request) {
		httpx.WriteOK(w, r, h.GetSequence(r.Context(), tenancy.MustFromContext(r.Context())))
	})

	g.Put("/barcode-sequence", func(w http.ResponseWriter, r *http.Request) {
		type updateRequest struct {
			NextValue                int64 `json:"next_value"`
			ClientAllocationRequired bool  `json:"client_allocation_required"`
		}
		body, derr := httpx.DecodeJSON[updateRequest](r)
		if derr != nil {
			httpx.WriteProblem(w, r, derr)
			return
		}
		httpx.WriteOK(w, r, h.UpdateSequence(r.Context(), tenancy.MustFromContext(r.Context()),
			body.NextValue, body.ClientAllocationRequired))
	})

	g.Post("/barcodes:allocate", func(w http.ResponseWriter, r *http.Request) {
		type allocateRequest struct {
			Count int `json:"count"`
		}
		body, derr := httpx.DecodeJSON[allocateRequest](r)
		if derr != nil {
			httpx.WriteProblem(w, r, derr)
			return
		}
		httpx.WriteOK(w, r, h.Allocate(r.Context(), tenancy.MustFromContext(r.Context()), body.Count))
	})

	g.Get("/barcode-allocations", func(w http.ResponseWriter, r *http.Request) {
		pr := httpx.QueryPagination(r)
		if pr.IsFailure() {
			httpx.WriteProblem(w, r, pr.Err())
			return
		}
		httpx.WriteOK(w, r, h.ListAllocations(r.Context(), tenancy.MustFromContext(r.Context()),
			pr.Value().Page, pr.Value().PageSize))
	})
}

// mountSettingsRoutes, katalog ayar uçlarını kaydeder
// (.NET CatalogSettingsEndpoints karşılığı): GET/PUT /settings.
func mountSettingsRoutes(g chi.Router, h *application.CatalogSettingsHandlers) {
	g.Get("/settings", func(w http.ResponseWriter, r *http.Request) {
		httpx.WriteOK(w, r, h.Get(r.Context(), tenancy.MustFromContext(r.Context())))
	})

	g.Put("/settings", func(w http.ResponseWriter, r *http.Request) {
		type updateRequest struct {
			SlicerNamePosition string `json:"slicer_name_position"`
		}
		body, derr := httpx.DecodeJSON[updateRequest](r)
		if derr != nil {
			httpx.WriteProblem(w, r, derr)
			return
		}
		httpx.WriteOK(w, r, h.Update(r.Context(), tenancy.MustFromContext(r.Context()), body.SlicerNamePosition))
	})
}
