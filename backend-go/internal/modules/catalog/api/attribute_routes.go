package api

import (
	"net/http"

	"github.com/go-chi/chi/v5"

	"pimly.commerslab/backend-go/internal/modules/catalog/application"
	"pimly.commerslab/backend-go/internal/platform/httpx"
	"pimly.commerslab/backend-go/internal/sharedkernel/tenancy"
)

// attributeRequest, özellik oluşturma/güncelleme isteklerinin kablo biçimidir.
type attributeRequest struct {
	Name string `json:"name"`
}

// attributeValueRequest, özellik değeri ekleme/güncelleme isteklerinin kablo biçimidir.
type attributeValueRequest struct {
	Name string `json:"name"`
}

// mountAttributeRoutes, özellik tanımı ve değer uçlarını kaydeder
// (.NET AttributeEndpoints + AttributeValueEndpoints karşılığı). Adlandırma
// asimetrisi bilinçlidir ve frontend'e yansımıştır: değer ekleme
// /attributes/{id}/values altında, değer güncelleme/silme üst seviyedeki
// /attribute-values/{id} altındadır.
func mountAttributeRoutes(g chi.Router, h *application.AttributeHandlers) {
	g.Post("/attributes", func(w http.ResponseWriter, r *http.Request) {
		body, derr := httpx.DecodeJSON[attributeRequest](r)
		if derr != nil {
			httpx.WriteProblem(w, r, derr)
			return
		}
		result := h.Create(r.Context(), tenancy.MustFromContext(r.Context()),
			application.CreateAttributeCommand{Name: body.Name})
		httpx.WriteCreated(w, r, result, func(dto application.AttributeDto) string {
			return "/api/v1/catalog/attributes/" + dto.ID.String()
		})
	})

	g.Get("/attributes", func(w http.ResponseWriter, r *http.Request) {
		pr := httpx.QueryPagination(r)
		if pr.IsFailure() {
			httpx.WriteProblem(w, r, pr.Err())
			return
		}
		result := h.List(r.Context(), tenancy.MustFromContext(r.Context()),
			pr.Value().Page, pr.Value().PageSize)
		httpx.WriteOK(w, r, result)
	})

	g.Get("/attributes/{id}", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		httpx.WriteOK(w, r, h.Get(r.Context(), tenancy.MustFromContext(r.Context()), id))
	})

	g.Patch("/attributes/{id}", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		body, derr := httpx.DecodeJSON[attributeRequest](r)
		if derr != nil {
			httpx.WriteProblem(w, r, derr)
			return
		}
		result := h.Update(r.Context(), tenancy.MustFromContext(r.Context()),
			application.UpdateAttributeCommand{ID: id, Name: body.Name})
		httpx.WriteOK(w, r, result)
	})

	g.Delete("/attributes/{id}", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		httpx.WriteResult(w, r, h.Delete(r.Context(), tenancy.MustFromContext(r.Context()), id))
	})

	g.Post("/attributes/{id}/values", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		body, derr := httpx.DecodeJSON[attributeValueRequest](r)
		if derr != nil {
			httpx.WriteProblem(w, r, derr)
			return
		}
		result := h.AddValue(r.Context(), tenancy.MustFromContext(r.Context()),
			application.AddAttributeValueCommand{AttributeID: id, Name: body.Name})
		httpx.WriteCreated(w, r, result, func(dto application.AttributeDefinitionValueDto) string {
			return "/api/v1/catalog/attribute-values/" + dto.ID.String()
		})
	})

	g.Get("/attributes/{id}/values", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		pr := httpx.QueryPagination(r)
		if pr.IsFailure() {
			httpx.WriteProblem(w, r, pr.Err())
			return
		}
		result := h.ListValues(r.Context(), tenancy.MustFromContext(r.Context()), id,
			pr.Value().Page, pr.Value().PageSize)
		httpx.WriteOK(w, r, result)
	})

	g.Patch("/attribute-values/{id}", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		body, derr := httpx.DecodeJSON[attributeValueRequest](r)
		if derr != nil {
			httpx.WriteProblem(w, r, derr)
			return
		}
		result := h.UpdateValue(r.Context(), tenancy.MustFromContext(r.Context()),
			application.UpdateAttributeValueCommand{ID: id, Name: body.Name})
		httpx.WriteOK(w, r, result)
	})

	g.Delete("/attribute-values/{id}", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		httpx.WriteResult(w, r, h.RemoveValue(r.Context(), tenancy.MustFromContext(r.Context()), id))
	})
}
