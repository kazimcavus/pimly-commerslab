package pimhttp

import (
	"net/http"

	"github.com/kazimcavus/pimly/internal/platform/db/tenantdb"
	"github.com/kazimcavus/pimly/internal/platform/httpx"
	"github.com/kazimcavus/pimly/internal/shared/apperr"
)

type categoryRequest struct {
	Name     string  `json:"name"`
	Code     *string `json:"code"`
	ParentID *string `json:"parent_id"`
}

func (h *Handler) CreateCategory(w http.ResponseWriter, r *http.Request) {
	var req categoryRequest
	if err := httpx.Decode(r, &req); err != nil {
		httpx.Error(w, r, err)
		return
	}
	if req.Name == "" {
		httpx.Error(w, r, apperr.Validation("name is required"))
		return
	}
	parent, err := optUUID(req.ParentID, "parent_id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	cat, err := inTenant(h, r, func(q *tenantdb.Queries) (tenantdb.Category, error) {
		return q.CreateCategory(r.Context(), tenantdb.CreateCategoryParams{
			ParentID: parent, Name: req.Name, Code: textPtr(req.Code),
		})
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusCreated, cat)
}

func (h *Handler) ListCategories(w http.ResponseWriter, r *http.Request) {
	cats, err := inTenant(h, r, func(q *tenantdb.Queries) ([]tenantdb.Category, error) {
		return q.ListCategories(r.Context())
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, cats)
}

func (h *Handler) GetCategory(w http.ResponseWriter, r *http.Request) {
	id, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	cat, err := inTenant(h, r, func(q *tenantdb.Queries) (tenantdb.Category, error) {
		return q.GetCategory(r.Context(), id)
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, cat)
}

func (h *Handler) UpdateCategory(w http.ResponseWriter, r *http.Request) {
	id, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	var req categoryRequest
	if err := httpx.Decode(r, &req); err != nil {
		httpx.Error(w, r, err)
		return
	}
	if req.Name == "" {
		httpx.Error(w, r, apperr.Validation("name is required"))
		return
	}
	parent, err := optUUID(req.ParentID, "parent_id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	cat, err := inTenant(h, r, func(q *tenantdb.Queries) (tenantdb.Category, error) {
		return q.UpdateCategory(r.Context(), tenantdb.UpdateCategoryParams{
			ID: id, ParentID: parent, Name: req.Name, Code: textPtr(req.Code),
		})
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, cat)
}

func (h *Handler) DeleteCategory(w http.ResponseWriter, r *http.Request) {
	id, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	rows, err := inTenant(h, r, func(q *tenantdb.Queries) (int64, error) {
		return q.DeleteCategory(r.Context(), id)
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	if rows == 0 {
		httpx.Error(w, r, apperr.NotFound("category not found"))
		return
	}
	w.WriteHeader(http.StatusNoContent)
}
