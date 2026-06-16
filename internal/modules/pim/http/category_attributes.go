package pimhttp

import (
	"net/http"

	"github.com/kazimcavus/pimly/internal/platform/db/tenantdb"
	"github.com/kazimcavus/pimly/internal/platform/httpx"
	"github.com/kazimcavus/pimly/internal/shared/apperr"
)

func (h *Handler) AssignCategoryAttribute(w http.ResponseWriter, r *http.Request) {
	catID, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	var req struct {
		AttributeID         string `json:"attribute_id"`
		Required            bool   `json:"required"`
		MarketplaceRequired bool   `json:"marketplace_required"`
		SortOrder           int32  `json:"sort_order"`
	}
	if err := httpx.Decode(r, &req); err != nil {
		httpx.Error(w, r, err)
		return
	}
	attrID, err := optUUID(&req.AttributeID, "attribute_id")
	if err != nil || attrID == nil {
		httpx.Error(w, r, apperr.Validation("attribute_id is required"))
		return
	}
	ca, err := inTenant(h, r, func(q *tenantdb.Queries) (tenantdb.CategoryAttribute, error) {
		return q.CreateCategoryAttribute(r.Context(), tenantdb.CreateCategoryAttributeParams{
			CategoryID: catID, AttributeID: *attrID,
			Required: req.Required, MarketplaceRequired: req.MarketplaceRequired, SortOrder: req.SortOrder,
		})
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusCreated, ca)
}

func (h *Handler) ListCategoryAttributes(w http.ResponseWriter, r *http.Request) {
	catID, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	rows, err := inTenant(h, r, func(q *tenantdb.Queries) ([]tenantdb.ListCategoryAttributeDefsRow, error) {
		return q.ListCategoryAttributeDefs(r.Context(), catID)
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, rows)
}

func (h *Handler) UpdateCategoryAttribute(w http.ResponseWriter, r *http.Request) {
	id, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	var req struct {
		Required            bool  `json:"required"`
		MarketplaceRequired bool  `json:"marketplace_required"`
		SortOrder           int32 `json:"sort_order"`
	}
	if err := httpx.Decode(r, &req); err != nil {
		httpx.Error(w, r, err)
		return
	}
	ca, err := inTenant(h, r, func(q *tenantdb.Queries) (tenantdb.CategoryAttribute, error) {
		return q.UpdateCategoryAttribute(r.Context(), tenantdb.UpdateCategoryAttributeParams{
			ID: id, Required: req.Required, MarketplaceRequired: req.MarketplaceRequired, SortOrder: req.SortOrder,
		})
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, ca)
}

func (h *Handler) DeleteCategoryAttribute(w http.ResponseWriter, r *http.Request) {
	id, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	rows, err := inTenant(h, r, func(q *tenantdb.Queries) (int64, error) {
		return q.DeleteCategoryAttribute(r.Context(), id)
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	if rows == 0 {
		httpx.Error(w, r, apperr.NotFound("category attribute not found"))
		return
	}
	w.WriteHeader(http.StatusNoContent)
}
