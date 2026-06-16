package pimhttp

import (
	"context"
	"encoding/json"
	"net/http"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"

	pimstore "github.com/kazimcavus/pimly/internal/modules/pim/store"
	"github.com/kazimcavus/pimly/internal/platform/db/tenantdb"
	"github.com/kazimcavus/pimly/internal/platform/httpx"
	"github.com/kazimcavus/pimly/internal/platform/tenant"
	"github.com/kazimcavus/pimly/internal/shared/apperr"
)

// CreateProductsBatch is the single write path: it creates a whole
// group→product→variant tree in one transaction.
func (h *Handler) CreateProductsBatch(w http.ResponseWriter, r *http.Request) {
	var in pimstore.BatchInput
	if err := httpx.Decode(r, &in); err != nil {
		httpx.Error(w, r, err)
		return
	}
	t, ok := tenant.FromContext(r.Context())
	if !ok {
		httpx.Error(w, r, apperr.Unauthorized("no tenant in context"))
		return
	}
	var res *pimstore.BatchResult
	err := h.db.WithTenant(r.Context(), t.SchemaName, func(tx pgx.Tx) error {
		var e error
		res, e = pimstore.CreateBatch(r.Context(), tx, t, in)
		return e
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusCreated, res)
}

// --- groups ---

type nestedGroup struct {
	tenantdb.Group
	Products []pimstore.ProductResult `json:"products"`
}

func (h *Handler) ListGroups(w http.ResponseWriter, r *http.Request) {
	groups, err := inTenant(h, r, func(q *tenantdb.Queries) ([]tenantdb.Group, error) {
		return q.ListGroups(r.Context())
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, groups)
}

func (h *Handler) GetGroup(w http.ResponseWriter, r *http.Request) {
	id, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	out, err := inTenant(h, r, func(q *tenantdb.Queries) (nestedGroup, error) {
		g, err := q.GetGroup(r.Context(), id)
		if err != nil {
			return nestedGroup{}, err
		}
		prods, err := q.ListProductsByGroup(r.Context(), id)
		if err != nil {
			return nestedGroup{}, err
		}
		res := nestedGroup{Group: g}
		for _, p := range prods {
			vs, err := q.ListVariantsByProduct(r.Context(), p.ID)
			if err != nil {
				return nestedGroup{}, err
			}
			res.Products = append(res.Products, pimstore.ProductResult{Product: p, Variants: vs})
		}
		return res, nil
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, out)
}

func (h *Handler) UpdateGroup(w http.ResponseWriter, r *http.Request) {
	id, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	var req struct {
		Title           *string         `json:"title"`
		Status          *string         `json:"status"`
		CategoryID      *string         `json:"category_id"`
		AttributeValues json.RawMessage `json:"attribute_values"`
	}
	if err := httpx.Decode(r, &req); err != nil {
		httpx.Error(w, r, err)
		return
	}
	g, err := inTenant(h, r, func(q *tenantdb.Queries) (tenantdb.Group, error) {
		cur, err := q.GetGroup(r.Context(), id)
		if err != nil {
			return tenantdb.Group{}, err
		}
		title := strOr(req.Title, cur.Title)
		status := strOr(req.Status, cur.Status)
		categoryID := cur.CategoryID
		if req.CategoryID != nil {
			categoryID, err = optUUID(req.CategoryID, "category_id")
			if err != nil {
				return tenantdb.Group{}, err
			}
		}
		attrs := cur.AttributeValues
		if len(req.AttributeValues) > 0 {
			attrs = normalizeJSON(req.AttributeValues)
		}
		if err := pimstore.ValidateAttrs(r.Context(), q, categoryID, "group", attrs, status == "active"); err != nil {
			return tenantdb.Group{}, err
		}
		return q.UpdateGroup(r.Context(), tenantdb.UpdateGroupParams{
			ID: id, Title: title, Status: status, CategoryID: categoryID, AttributeValues: attrs,
		})
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, g)
}

func (h *Handler) DeleteGroup(w http.ResponseWriter, r *http.Request) {
	h.deleteByID(w, r, "group", func(q *tenantdb.Queries, id uuid.UUID) (int64, error) {
		return q.DeleteGroup(r.Context(), id)
	})
}

// --- products ---

func (h *Handler) GetProduct(w http.ResponseWriter, r *http.Request) {
	id, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	p, err := inTenant(h, r, func(q *tenantdb.Queries) (tenantdb.Product, error) {
		return q.GetProduct(r.Context(), id)
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, p)
}

func (h *Handler) UpdateProduct(w http.ResponseWriter, r *http.Request) {
	id, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	var req struct {
		Title                *string         `json:"title"`
		Status               *string         `json:"status"`
		GroupingValueEntryID *string         `json:"grouping_value_entry_id"`
		AttributeValues      json.RawMessage `json:"attribute_values"`
	}
	if err := httpx.Decode(r, &req); err != nil {
		httpx.Error(w, r, err)
		return
	}
	p, err := inTenant(h, r, func(q *tenantdb.Queries) (tenantdb.Product, error) {
		cur, err := q.GetProduct(r.Context(), id)
		if err != nil {
			return tenantdb.Product{}, err
		}
		categoryID, err := categoryForGroup(r.Context(), q, cur.GroupID)
		if err != nil {
			return tenantdb.Product{}, err
		}
		title := strOr(req.Title, cur.Title)
		status := strOr(req.Status, cur.Status)
		grouping := cur.GroupingValueEntryID
		if req.GroupingValueEntryID != nil {
			grouping, err = optUUID(req.GroupingValueEntryID, "grouping_value_entry_id")
			if err != nil {
				return tenantdb.Product{}, err
			}
		}
		attrs := cur.AttributeValues
		if len(req.AttributeValues) > 0 {
			attrs = normalizeJSON(req.AttributeValues)
		}
		if err := pimstore.ValidateAttrs(r.Context(), q, categoryID, "product", attrs, status == "active"); err != nil {
			return tenantdb.Product{}, err
		}
		return q.UpdateProduct(r.Context(), tenantdb.UpdateProductParams{
			ID: id, Title: title, Status: status, AttributeValues: attrs, GroupingValueEntryID: grouping,
		})
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, p)
}

func (h *Handler) DeleteProduct(w http.ResponseWriter, r *http.Request) {
	h.deleteByID(w, r, "product", func(q *tenantdb.Queries, id uuid.UUID) (int64, error) {
		return q.DeleteProduct(r.Context(), id)
	})
}

// --- variants ---

func (h *Handler) GetVariant(w http.ResponseWriter, r *http.Request) {
	id, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	v, err := inTenant(h, r, func(q *tenantdb.Queries) (tenantdb.Variant, error) {
		return q.GetVariant(r.Context(), id)
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, v)
}

func (h *Handler) UpdateVariant(w http.ResponseWriter, r *http.Request) {
	id, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	var req struct {
		Gtin             *string         `json:"gtin"`
		Mpn              *string         `json:"mpn"`
		AxisValue        *string         `json:"axis_value"`
		AxisValueEntryID *string         `json:"axis_value_entry_id"`
		Price            *float64        `json:"price"`
		CompareAtPrice   *float64        `json:"compare_at_price"`
		Stock            *int32          `json:"stock"`
		AttributeValues  json.RawMessage `json:"attribute_values"`
	}
	if err := httpx.Decode(r, &req); err != nil {
		httpx.Error(w, r, err)
		return
	}
	v, err := inTenant(h, r, func(q *tenantdb.Queries) (tenantdb.Variant, error) {
		cur, err := q.GetVariant(r.Context(), id)
		if err != nil {
			return tenantdb.Variant{}, err
		}
		prod, err := q.GetProduct(r.Context(), cur.ProductID)
		if err != nil {
			return tenantdb.Variant{}, err
		}
		categoryID, err := categoryForGroup(r.Context(), q, prod.GroupID)
		if err != nil {
			return tenantdb.Variant{}, err
		}
		params := tenantdb.UpdateVariantParams{
			ID:               id,
			Gtin:             cur.Gtin,
			Mpn:              cur.Mpn,
			AxisValueEntryID: cur.AxisValueEntryID,
			AxisValue:        cur.AxisValue,
			Price:            cur.Price,
			CompareAtPrice:   cur.CompareAtPrice,
			Stock:            cur.Stock,
			AttributeValues:  cur.AttributeValues,
		}
		if req.Gtin != nil {
			params.Gtin = textOrNull(*req.Gtin)
		}
		if req.Mpn != nil {
			params.Mpn = textOrNull(*req.Mpn)
		}
		if req.AxisValue != nil {
			params.AxisValue = textOrNull(*req.AxisValue)
		}
		if req.AxisValueEntryID != nil {
			params.AxisValueEntryID, err = optUUID(req.AxisValueEntryID, "axis_value_entry_id")
			if err != nil {
				return tenantdb.Variant{}, err
			}
		}
		if req.Price != nil {
			if params.Price, err = numeric(*req.Price); err != nil {
				return tenantdb.Variant{}, err
			}
		}
		if req.CompareAtPrice != nil {
			if params.CompareAtPrice, err = nullableNumeric(req.CompareAtPrice); err != nil {
				return tenantdb.Variant{}, err
			}
		}
		if req.Stock != nil {
			params.Stock = *req.Stock
		}
		if len(req.AttributeValues) > 0 {
			params.AttributeValues = normalizeJSON(req.AttributeValues)
		}
		if err := pimstore.ValidateAttrs(r.Context(), q, categoryID, "variant", params.AttributeValues, prod.Status == "active"); err != nil {
			return tenantdb.Variant{}, err
		}
		return q.UpdateVariant(r.Context(), params)
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, v)
}

func (h *Handler) DeleteVariant(w http.ResponseWriter, r *http.Request) {
	h.deleteByID(w, r, "variant", func(q *tenantdb.Queries, id uuid.UUID) (int64, error) {
		return q.DeleteVariant(r.Context(), id)
	})
}

// --- shared helpers ---

func (h *Handler) deleteByID(w http.ResponseWriter, r *http.Request, entity string, del func(*tenantdb.Queries, uuid.UUID) (int64, error)) {
	id, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	rows, err := inTenant(h, r, func(q *tenantdb.Queries) (int64, error) {
		return del(q, id)
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	if rows == 0 {
		httpx.Error(w, r, apperr.NotFound("%s not found", entity))
		return
	}
	w.WriteHeader(http.StatusNoContent)
}

func categoryForGroup(ctx context.Context, q *tenantdb.Queries, groupID uuid.UUID) (*uuid.UUID, error) {
	g, err := q.GetGroup(ctx, groupID)
	if err != nil {
		return nil, err
	}
	return g.CategoryID, nil
}

func strOr(p *string, def string) string {
	if p != nil {
		return *p
	}
	return def
}
