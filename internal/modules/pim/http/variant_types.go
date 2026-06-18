package pimhttp

import (
	"net/http"

	"github.com/kazimcavus/pimly/internal/platform/db/tenantdb"
	"github.com/kazimcavus/pimly/internal/platform/httpx"
	"github.com/kazimcavus/pimly/internal/shared/apperr"
)

// A variant type is a reusable option axis (Renk, Beden, Ölçü) with a selection
// style ('list' or 'color') and a set of values. Products pick 1–3 of these to
// generate their variant rows.

// --- variant types ---

func (h *Handler) CreateVariantType(w http.ResponseWriter, r *http.Request) {
	var req struct {
		Name           string `json:"name"`
		SelectionStyle string `json:"selection_style"`
		SortOrder      int32  `json:"sort_order"`
	}
	if err := httpx.Decode(r, &req); err != nil {
		httpx.Error(w, r, err)
		return
	}
	if req.Name == "" {
		httpx.Error(w, r, apperr.Validation("name is required"))
		return
	}
	if req.SelectionStyle == "" {
		req.SelectionStyle = "list"
	}
	t, err := inTenant(h, r, func(q *tenantdb.Queries) (tenantdb.VariantType, error) {
		return q.CreateVariantType(r.Context(), tenantdb.CreateVariantTypeParams{
			Name: req.Name, SelectionStyle: req.SelectionStyle, SortOrder: req.SortOrder,
		})
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusCreated, t)
}

func (h *Handler) ListVariantTypes(w http.ResponseWriter, r *http.Request) {
	types, err := inTenant(h, r, func(q *tenantdb.Queries) ([]tenantdb.VariantType, error) {
		return q.ListVariantTypes(r.Context())
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, types)
}

func (h *Handler) GetVariantType(w http.ResponseWriter, r *http.Request) {
	id, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	t, err := inTenant(h, r, func(q *tenantdb.Queries) (tenantdb.VariantType, error) {
		return q.GetVariantTypeByID(r.Context(), id)
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, t)
}

func (h *Handler) UpdateVariantType(w http.ResponseWriter, r *http.Request) {
	id, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	var req struct {
		Name           string `json:"name"`
		SelectionStyle string `json:"selection_style"`
		SortOrder      int32  `json:"sort_order"`
	}
	if err := httpx.Decode(r, &req); err != nil {
		httpx.Error(w, r, err)
		return
	}
	if req.Name == "" {
		httpx.Error(w, r, apperr.Validation("name is required"))
		return
	}
	if req.SelectionStyle == "" {
		req.SelectionStyle = "list"
	}
	t, err := inTenant(h, r, func(q *tenantdb.Queries) (tenantdb.VariantType, error) {
		return q.UpdateVariantType(r.Context(), tenantdb.UpdateVariantTypeParams{
			ID: id, Name: req.Name, SelectionStyle: req.SelectionStyle, SortOrder: req.SortOrder,
		})
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, t)
}

func (h *Handler) DeleteVariantType(w http.ResponseWriter, r *http.Request) {
	id, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	rows, err := inTenant(h, r, func(q *tenantdb.Queries) (int64, error) {
		return q.DeleteVariantType(r.Context(), id)
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	if rows == 0 {
		httpx.Error(w, r, apperr.NotFound("variant type not found"))
		return
	}
	w.WriteHeader(http.StatusNoContent)
}

// --- variant values ---

func (h *Handler) CreateVariantValue(w http.ResponseWriter, r *http.Request) {
	typeID, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	var req struct {
		Label     string  `json:"label"`
		Color     *string `json:"color"`
		ImageURL  *string `json:"image_url"`
		Code      *string `json:"code"`
		SortOrder int32   `json:"sort_order"`
	}
	if err := httpx.Decode(r, &req); err != nil {
		httpx.Error(w, r, err)
		return
	}
	if req.Label == "" {
		httpx.Error(w, r, apperr.Validation("label is required"))
		return
	}
	v, err := inTenant(h, r, func(q *tenantdb.Queries) (tenantdb.VariantValue, error) {
		return q.CreateVariantValue(r.Context(), tenantdb.CreateVariantValueParams{
			VariantTypeID: typeID, Label: req.Label,
			Color: textPtr(req.Color), ImageUrl: textPtr(req.ImageURL), Code: textPtr(req.Code), SortOrder: req.SortOrder,
		})
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusCreated, v)
}

func (h *Handler) ListVariantValues(w http.ResponseWriter, r *http.Request) {
	typeID, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	values, err := inTenant(h, r, func(q *tenantdb.Queries) ([]tenantdb.VariantValue, error) {
		return q.ListVariantValues(r.Context(), typeID)
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, values)
}

func (h *Handler) UpdateVariantValue(w http.ResponseWriter, r *http.Request) {
	id, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	var req struct {
		Label     string  `json:"label"`
		Color     *string `json:"color"`
		ImageURL  *string `json:"image_url"`
		Code      *string `json:"code"`
		SortOrder int32   `json:"sort_order"`
	}
	if err := httpx.Decode(r, &req); err != nil {
		httpx.Error(w, r, err)
		return
	}
	if req.Label == "" {
		httpx.Error(w, r, apperr.Validation("label is required"))
		return
	}
	v, err := inTenant(h, r, func(q *tenantdb.Queries) (tenantdb.VariantValue, error) {
		return q.UpdateVariantValue(r.Context(), tenantdb.UpdateVariantValueParams{
			ID: id, Label: req.Label,
			Color: textPtr(req.Color), ImageUrl: textPtr(req.ImageURL), Code: textPtr(req.Code), SortOrder: req.SortOrder,
		})
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, v)
}

func (h *Handler) DeleteVariantValue(w http.ResponseWriter, r *http.Request) {
	id, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	rows, err := inTenant(h, r, func(q *tenantdb.Queries) (int64, error) {
		return q.DeleteVariantValue(r.Context(), id)
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	if rows == 0 {
		httpx.Error(w, r, apperr.NotFound("variant value not found"))
		return
	}
	w.WriteHeader(http.StatusNoContent)
}
