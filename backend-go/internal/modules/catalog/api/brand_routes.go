package api

import (
	"net/http"

	"github.com/go-chi/chi/v5"
	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/catalog/application"
	"pimly.commerslab/backend-go/internal/platform/httpx"
	"pimly.commerslab/backend-go/internal/sharedkernel/tenancy"
)

// brandRequest, marka oluşturma/güncelleme isteklerinin kablo biçimidir;
// code gönderilmeyebilir veya null olabilir.
type brandRequest struct {
	Name string  `json:"name"`
	Code *string `json:"code"`
}

// mountBrandRoutes, marka uçlarını kaydeder (.NET BrandEndpoints karşılığı):
// POST/GET /brands, GET/PATCH/DELETE /brands/{id}.
func mountBrandRoutes(g chi.Router, h *application.BrandHandlers) {
	g.Post("/brands", func(w http.ResponseWriter, r *http.Request) {
		body, derr := httpx.DecodeJSON[brandRequest](r)
		if derr != nil {
			httpx.WriteProblem(w, r, derr)
			return
		}
		result := h.Create(r.Context(), tenancy.MustFromContext(r.Context()),
			application.CreateBrandCommand{Name: body.Name, Code: body.Code})
		httpx.WriteCreated(w, r, result, func(dto application.BrandDto) string {
			return "/api/v1/catalog/brands/" + dto.ID.String()
		})
	})

	g.Get("/brands", func(w http.ResponseWriter, r *http.Request) {
		pr := httpx.QueryPagination(r)
		if pr.IsFailure() {
			httpx.WriteProblem(w, r, pr.Err())
			return
		}
		result := h.List(r.Context(), tenancy.MustFromContext(r.Context()),
			pr.Value().Page, pr.Value().PageSize)
		httpx.WriteOK(w, r, result)
	})

	g.Get("/brands/{id}", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		httpx.WriteOK(w, r, h.Get(r.Context(), tenancy.MustFromContext(r.Context()), id))
	})

	g.Patch("/brands/{id}", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		body, derr := httpx.DecodeJSON[brandRequest](r)
		if derr != nil {
			httpx.WriteProblem(w, r, derr)
			return
		}
		result := h.Update(r.Context(), tenancy.MustFromContext(r.Context()),
			application.UpdateBrandCommand{ID: id, Name: body.Name, Code: body.Code})
		httpx.WriteOK(w, r, result)
	})

	g.Delete("/brands/{id}", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		httpx.WriteResult(w, r, h.Delete(r.Context(), tenancy.MustFromContext(r.Context()), id))
	})
}

// pathUUID, {param} yol değişkenini UUID olarak çözer; geçersizse .NET'in
// {id:guid} rota kısıtı gibi gövdesiz 404 yazar ve false döner.
func pathUUID(w http.ResponseWriter, r *http.Request, name string) (uuid.UUID, bool) {
	id, err := uuid.Parse(chi.URLParam(r, name))
	if err != nil {
		w.WriteHeader(http.StatusNotFound)
		return uuid.Nil, false
	}
	return id, true
}
