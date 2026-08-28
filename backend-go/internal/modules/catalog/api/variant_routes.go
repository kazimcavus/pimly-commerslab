package api

import (
	"net/http"

	"github.com/go-chi/chi/v5"

	"pimly.commerslab/backend-go/internal/modules/catalog/application"
	"pimly.commerslab/backend-go/internal/platform/httpx"
	"pimly.commerslab/backend-go/internal/sharedkernel/tenancy"
)

// variantTypeRequest, varyant türü oluşturma/güncelleme isteklerinin kablo
// biçimidir; selection_style boşsa "list" varsayılır, key yalnızca oluşturmada
// dikkate alınır.
type variantTypeRequest struct {
	Name           string  `json:"name"`
	SelectionStyle *string `json:"selection_style"`
	SortOrder      int     `json:"sort_order"`
	Slicer         bool    `json:"slicer"`
	Key            *string `json:"key"`
}

// variantValueRequest, varyant değeri ekleme/güncelleme isteklerinin kablo biçimidir.
type variantValueRequest struct {
	Label     string  `json:"label"`
	Color     *string `json:"color"`
	ImageURL  *string `json:"image_url"`
	Key       *string `json:"key"`
	SortOrder int     `json:"sort_order"`
}

// selectionStyleOrDefault, null selection_style'ı .NET'in `?? "list"`
// davranışıyla eşler.
func selectionStyleOrDefault(s *string) string {
	if s == nil {
		return "list"
	}
	return *s
}

// mountVariantRoutes, varyant türü ve değeri uçlarını kaydeder
// (.NET VariantTypeEndpoints + VariantValueEndpoints karşılığı). Türler
// /variants altındadır (tarihsel adlandırma — /variant-types değil).
func mountVariantRoutes(g chi.Router, h *application.VariantHandlers) {
	g.Post("/variants", func(w http.ResponseWriter, r *http.Request) {
		body, derr := httpx.DecodeJSON[variantTypeRequest](r)
		if derr != nil {
			httpx.WriteProblem(w, r, derr)
			return
		}
		result := h.Create(r.Context(), tenancy.MustFromContext(r.Context()),
			application.CreateVariantTypeCommand{
				Name:           body.Name,
				SelectionStyle: selectionStyleOrDefault(body.SelectionStyle),
				SortOrder:      body.SortOrder,
				Slicer:         body.Slicer,
				Key:            body.Key,
			})
		httpx.WriteCreated(w, r, result, func(dto application.VariantTypeDto) string {
			return "/api/v1/catalog/variants/" + dto.ID.String()
		})
	})

	g.Get("/variants", func(w http.ResponseWriter, r *http.Request) {
		pr := httpx.QueryPagination(r)
		if pr.IsFailure() {
			httpx.WriteProblem(w, r, pr.Err())
			return
		}
		result := h.List(r.Context(), tenancy.MustFromContext(r.Context()),
			pr.Value().Page, pr.Value().PageSize)
		httpx.WriteOK(w, r, result)
	})

	g.Get("/variants/{id}", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		httpx.WriteOK(w, r, h.Get(r.Context(), tenancy.MustFromContext(r.Context()), id))
	})

	g.Patch("/variants/{id}", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		body, derr := httpx.DecodeJSON[variantTypeRequest](r)
		if derr != nil {
			httpx.WriteProblem(w, r, derr)
			return
		}
		result := h.Update(r.Context(), tenancy.MustFromContext(r.Context()),
			application.UpdateVariantTypeCommand{
				ID:             id,
				Name:           body.Name,
				SelectionStyle: selectionStyleOrDefault(body.SelectionStyle),
				SortOrder:      body.SortOrder,
				Slicer:         body.Slicer,
			})
		httpx.WriteOK(w, r, result)
	})

	g.Delete("/variants/{id}", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		httpx.WriteResult(w, r, h.Delete(r.Context(), tenancy.MustFromContext(r.Context()), id))
	})

	g.Post("/variants/{id}/values", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		body, derr := httpx.DecodeJSON[variantValueRequest](r)
		if derr != nil {
			httpx.WriteProblem(w, r, derr)
			return
		}
		result := h.AddValue(r.Context(), tenancy.MustFromContext(r.Context()),
			application.VariantValueCommand{
				VariantTypeID: id,
				Label:         body.Label,
				Color:         body.Color,
				ImageURL:      body.ImageURL,
				Key:           body.Key,
				SortOrder:     body.SortOrder,
			})
		httpx.WriteCreated(w, r, result, func(dto application.VariantValueDto) string {
			return "/api/v1/catalog/variant-values/" + dto.ID.String()
		})
	})

	g.Get("/variants/{id}/values", func(w http.ResponseWriter, r *http.Request) {
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

	g.Patch("/variant-values/{id}", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		body, derr := httpx.DecodeJSON[variantValueRequest](r)
		if derr != nil {
			httpx.WriteProblem(w, r, derr)
			return
		}
		result := h.UpdateValue(r.Context(), tenancy.MustFromContext(r.Context()),
			application.VariantValueCommand{
				ValueID:   id,
				Label:     body.Label,
				Color:     body.Color,
				ImageURL:  body.ImageURL,
				Key:       body.Key,
				SortOrder: body.SortOrder,
			})
		httpx.WriteOK(w, r, result)
	})

	g.Delete("/variant-values/{id}", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		httpx.WriteResult(w, r, h.RemoveValue(r.Context(), tenancy.MustFromContext(r.Context()), id))
	})
}
