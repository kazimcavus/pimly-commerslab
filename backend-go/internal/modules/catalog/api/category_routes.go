package api

import (
	"net/http"

	"github.com/go-chi/chi/v5"
	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/catalog/application"
	"pimly.commerslab/backend-go/internal/modules/catalog/domain/categories"
	"pimly.commerslab/backend-go/internal/platform/httpx"
	"pimly.commerslab/backend-go/internal/sharedkernel/tenancy"
)

// categoryRequest, kategori oluşturma/güncelleme isteklerinin kablo biçimidir.
type categoryRequest struct {
	Name     string     `json:"name"`
	Code     *string    `json:"code"`
	ParentID *uuid.UUID `json:"parent_id"`
}

// assignAttributeRequest, kategoriye özellik atama isteğinin kablo biçimidir;
// scope "model" | "slicer" | "item" (boş/tanınmayan değer model'e düşer).
type assignAttributeRequest struct {
	AttributeID uuid.UUID `json:"attribute_id"`
	Required    bool      `json:"required"`
	SortOrder   int       `json:"sort_order"`
	Scope       string    `json:"scope"`
}

// updateAssignmentRequest, atama güncelleme isteğinin kablo biçimidir;
// tanınmayan scope mevcut seviyeyi korur.
type updateAssignmentRequest struct {
	Required  bool   `json:"required"`
	SortOrder int    `json:"sort_order"`
	Scope     string `json:"scope"`
}

// mountCategoryRoutes, kategori ve kategori-özellik ataması uçlarını kaydeder
// (.NET CategoryEndpoints karşılığı).
func mountCategoryRoutes(g chi.Router, h *application.CategoryHandlers) {
	g.Post("/categories", func(w http.ResponseWriter, r *http.Request) {
		body, derr := httpx.DecodeJSON[categoryRequest](r)
		if derr != nil {
			httpx.WriteProblem(w, r, derr)
			return
		}
		result := h.Create(r.Context(), tenancy.MustFromContext(r.Context()),
			application.CreateCategoryCommand{Name: body.Name, Code: body.Code, ParentID: body.ParentID})
		httpx.WriteCreated(w, r, result, func(dto application.CategoryDto) string {
			return "/api/v1/catalog/categories/" + dto.ID.String()
		})
	})

	g.Get("/categories", func(w http.ResponseWriter, r *http.Request) {
		pr := httpx.QueryPagination(r)
		if pr.IsFailure() {
			httpx.WriteProblem(w, r, pr.Err())
			return
		}
		result := h.List(r.Context(), tenancy.MustFromContext(r.Context()),
			pr.Value().Page, pr.Value().PageSize)
		httpx.WriteOK(w, r, result)
	})

	g.Get("/categories/{id}", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		httpx.WriteOK(w, r, h.Get(r.Context(), tenancy.MustFromContext(r.Context()), id))
	})

	g.Patch("/categories/{id}", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		body, derr := httpx.DecodeJSON[categoryRequest](r)
		if derr != nil {
			httpx.WriteProblem(w, r, derr)
			return
		}
		result := h.Update(r.Context(), tenancy.MustFromContext(r.Context()),
			application.UpdateCategoryCommand{ID: id, Name: body.Name, Code: body.Code, ParentID: body.ParentID})
		httpx.WriteOK(w, r, result)
	})

	g.Delete("/categories/{id}", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		httpx.WriteResult(w, r, h.Delete(r.Context(), tenancy.MustFromContext(r.Context()), id))
	})

	g.Post("/categories/{id}/attributes", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		body, derr := httpx.DecodeJSON[assignAttributeRequest](r)
		if derr != nil {
			httpx.WriteProblem(w, r, derr)
			return
		}
		scope, parsed := categories.ParseScope(body.Scope)
		if !parsed {
			scope = categories.ScopeModel
		}
		result := h.AssignAttribute(r.Context(), tenancy.MustFromContext(r.Context()),
			application.AssignCategoryAttributeCommand{
				CategoryID:  id,
				AttributeID: body.AttributeID,
				Required:    body.Required,
				SortOrder:   body.SortOrder,
				Scope:       scope,
			})
		httpx.WriteCreated(w, r, result, func(dto application.CategoryAttributeDto) string {
			return "/api/v1/catalog/category-attributes/" + dto.CategoryAttributeID.String()
		})
	})

	g.Get("/categories/{id}/attributes", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		pr := httpx.QueryPagination(r)
		if pr.IsFailure() {
			httpx.WriteProblem(w, r, pr.Err())
			return
		}
		result := h.ListAttributes(r.Context(), tenancy.MustFromContext(r.Context()), id,
			pr.Value().Page, pr.Value().PageSize)
		httpx.WriteOK(w, r, result)
	})

	g.Patch("/category-attributes/{id}", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		body, derr := httpx.DecodeJSON[updateAssignmentRequest](r)
		if derr != nil {
			httpx.WriteProblem(w, r, derr)
			return
		}
		var scopePtr *categories.AttributeScope
		if scope, parsed := categories.ParseScope(body.Scope); parsed {
			scopePtr = &scope
		}
		result := h.UpdateAssignment(r.Context(), tenancy.MustFromContext(r.Context()),
			application.UpdateCategoryAttributeCommand{
				ID:        id,
				Required:  body.Required,
				SortOrder: body.SortOrder,
				Scope:     scopePtr,
			})
		httpx.WriteOK(w, r, result)
	})

	g.Delete("/category-attributes/{id}", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		httpx.WriteResult(w, r, h.RemoveAssignment(r.Context(), tenancy.MustFromContext(r.Context()), id))
	})
}
