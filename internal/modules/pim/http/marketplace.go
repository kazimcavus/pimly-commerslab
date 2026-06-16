package pimhttp

import (
	"net/http"

	"github.com/kazimcavus/pimly/internal/platform/db/tenantdb"
	"github.com/kazimcavus/pimly/internal/platform/httpx"
	"github.com/kazimcavus/pimly/internal/shared/apperr"
)

// --- marketplace category map ---

func (h *Handler) UpsertMarketplaceCategoryMap(w http.ResponseWriter, r *http.Request) {
	catID, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	var req struct {
		Marketplace             string  `json:"marketplace"`
		MarketplaceCategoryID   *string `json:"marketplace_category_id"`
		MarketplaceCategoryPath *string `json:"marketplace_category_path"`
	}
	if err := httpx.Decode(r, &req); err != nil {
		httpx.Error(w, r, err)
		return
	}
	if req.Marketplace == "" {
		httpx.Error(w, r, apperr.Validation("marketplace is required"))
		return
	}
	m, err := inTenant(h, r, func(q *tenantdb.Queries) (tenantdb.MarketplaceCategoryMap, error) {
		return q.UpsertMarketplaceCategoryMap(r.Context(), tenantdb.UpsertMarketplaceCategoryMapParams{
			CategoryID: catID, Marketplace: req.Marketplace,
			MarketplaceCategoryID:   textPtr(req.MarketplaceCategoryID),
			MarketplaceCategoryPath: textPtr(req.MarketplaceCategoryPath),
		})
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, m)
}

func (h *Handler) ListMarketplaceCategoryMaps(w http.ResponseWriter, r *http.Request) {
	catID, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	rows, err := inTenant(h, r, func(q *tenantdb.Queries) ([]tenantdb.MarketplaceCategoryMap, error) {
		return q.ListMarketplaceCategoryMaps(r.Context(), catID)
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, rows)
}

func (h *Handler) DeleteMarketplaceCategoryMap(w http.ResponseWriter, r *http.Request) {
	id, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	rows, err := inTenant(h, r, func(q *tenantdb.Queries) (int64, error) {
		return q.DeleteMarketplaceCategoryMap(r.Context(), id)
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	if rows == 0 {
		httpx.Error(w, r, apperr.NotFound("marketplace category map not found"))
		return
	}
	w.WriteHeader(http.StatusNoContent)
}

// --- marketplace attribute map ---

func (h *Handler) UpsertMarketplaceAttributeMap(w http.ResponseWriter, r *http.Request) {
	catID, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	var req struct {
		AttributeID              string  `json:"attribute_id"`
		Marketplace              string  `json:"marketplace"`
		MarketplaceAttributeID   *string `json:"marketplace_attribute_id"`
		MarketplaceAttributeName *string `json:"marketplace_attribute_name"`
		Required                 bool    `json:"required"`
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
	if req.Marketplace == "" {
		httpx.Error(w, r, apperr.Validation("marketplace is required"))
		return
	}
	m, err := inTenant(h, r, func(q *tenantdb.Queries) (tenantdb.MarketplaceAttributeMap, error) {
		return q.UpsertMarketplaceAttributeMap(r.Context(), tenantdb.UpsertMarketplaceAttributeMapParams{
			CategoryID: catID, AttributeID: *attrID, Marketplace: req.Marketplace,
			MarketplaceAttributeID:   textPtr(req.MarketplaceAttributeID),
			MarketplaceAttributeName: textPtr(req.MarketplaceAttributeName),
			Required:                 req.Required,
		})
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, m)
}

func (h *Handler) ListMarketplaceAttributeMaps(w http.ResponseWriter, r *http.Request) {
	catID, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	rows, err := inTenant(h, r, func(q *tenantdb.Queries) ([]tenantdb.MarketplaceAttributeMap, error) {
		return q.ListMarketplaceAttributeMaps(r.Context(), catID)
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, rows)
}

func (h *Handler) DeleteMarketplaceAttributeMap(w http.ResponseWriter, r *http.Request) {
	id, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	rows, err := inTenant(h, r, func(q *tenantdb.Queries) (int64, error) {
		return q.DeleteMarketplaceAttributeMap(r.Context(), id)
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	if rows == 0 {
		httpx.Error(w, r, apperr.NotFound("marketplace attribute map not found"))
		return
	}
	w.WriteHeader(http.StatusNoContent)
}
