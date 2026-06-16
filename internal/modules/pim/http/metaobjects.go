package pimhttp

import (
	"encoding/json"
	"net/http"

	"github.com/google/uuid"

	"github.com/kazimcavus/pimly/internal/platform/db/tenantdb"
	"github.com/kazimcavus/pimly/internal/platform/httpx"
	"github.com/kazimcavus/pimly/internal/shared/apperr"
	"github.com/kazimcavus/pimly/internal/shared/validation"
)

// --- definitions ---

func (h *Handler) CreateMetaobjectDefinition(w http.ResponseWriter, r *http.Request) {
	var req struct {
		Key   string `json:"key"`
		Label string `json:"label"`
	}
	if err := httpx.Decode(r, &req); err != nil {
		httpx.Error(w, r, err)
		return
	}
	if req.Key == "" || req.Label == "" {
		httpx.Error(w, r, apperr.Validation("key and label are required"))
		return
	}
	def, err := inTenant(h, r, func(q *tenantdb.Queries) (tenantdb.MetaobjectDefinition, error) {
		return q.CreateMetaobjectDefinition(r.Context(), tenantdb.CreateMetaobjectDefinitionParams{Key: req.Key, Label: req.Label})
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusCreated, def)
}

func (h *Handler) ListMetaobjectDefinitions(w http.ResponseWriter, r *http.Request) {
	defs, err := inTenant(h, r, func(q *tenantdb.Queries) ([]tenantdb.MetaobjectDefinition, error) {
		return q.ListMetaobjectDefinitions(r.Context())
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, defs)
}

func (h *Handler) GetMetaobjectDefinition(w http.ResponseWriter, r *http.Request) {
	id, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	def, err := inTenant(h, r, func(q *tenantdb.Queries) (tenantdb.MetaobjectDefinition, error) {
		return q.GetMetaobjectDefinitionByID(r.Context(), id)
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, def)
}

func (h *Handler) DeleteMetaobjectDefinition(w http.ResponseWriter, r *http.Request) {
	id, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	rows, err := inTenant(h, r, func(q *tenantdb.Queries) (int64, error) {
		return q.DeleteMetaobjectDefinition(r.Context(), id)
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	if rows == 0 {
		httpx.Error(w, r, apperr.NotFound("metaobject definition not found"))
		return
	}
	w.WriteHeader(http.StatusNoContent)
}

// --- fields ---

func (h *Handler) CreateMetaobjectField(w http.ResponseWriter, r *http.Request) {
	defID, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	var req struct {
		Key      string `json:"key"`
		Label    string `json:"label"`
		DataType string `json:"data_type"`
	}
	if err := httpx.Decode(r, &req); err != nil {
		httpx.Error(w, r, err)
		return
	}
	if req.Key == "" || req.Label == "" || req.DataType == "" {
		httpx.Error(w, r, apperr.Validation("key, label and data_type are required"))
		return
	}
	field, err := inTenant(h, r, func(q *tenantdb.Queries) (tenantdb.MetaobjectField, error) {
		return q.CreateMetaobjectField(r.Context(), tenantdb.CreateMetaobjectFieldParams{
			DefinitionID: defID, Key: req.Key, Label: req.Label, DataType: req.DataType,
		})
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusCreated, field)
}

func (h *Handler) ListMetaobjectFields(w http.ResponseWriter, r *http.Request) {
	defID, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	fields, err := inTenant(h, r, func(q *tenantdb.Queries) ([]tenantdb.MetaobjectField, error) {
		return q.ListMetaobjectFields(r.Context(), defID)
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, fields)
}

func (h *Handler) DeleteMetaobjectField(w http.ResponseWriter, r *http.Request) {
	id, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	rows, err := inTenant(h, r, func(q *tenantdb.Queries) (int64, error) {
		return q.DeleteMetaobjectField(r.Context(), id)
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	if rows == 0 {
		httpx.Error(w, r, apperr.NotFound("metaobject field not found"))
		return
	}
	w.WriteHeader(http.StatusNoContent)
}

// --- entries ---

func (h *Handler) CreateMetaobjectEntry(w http.ResponseWriter, r *http.Request) {
	defID, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	var req struct {
		Values map[string]json.RawMessage `json:"values"`
	}
	if err := httpx.Decode(r, &req); err != nil {
		httpx.Error(w, r, err)
		return
	}
	entry, err := inTenant(h, r, func(q *tenantdb.Queries) (tenantdb.MetaobjectEntry, error) {
		raw, err := validateEntryValues(r, q, defID, req.Values)
		if err != nil {
			return tenantdb.MetaobjectEntry{}, err
		}
		return q.CreateMetaobjectEntry(r.Context(), tenantdb.CreateMetaobjectEntryParams{DefinitionID: defID, Values: raw})
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusCreated, entry)
}

func (h *Handler) ListMetaobjectEntries(w http.ResponseWriter, r *http.Request) {
	defID, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	entries, err := inTenant(h, r, func(q *tenantdb.Queries) ([]tenantdb.MetaobjectEntry, error) {
		return q.ListMetaobjectEntries(r.Context(), defID)
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, entries)
}

func (h *Handler) GetMetaobjectEntry(w http.ResponseWriter, r *http.Request) {
	id, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	entry, err := inTenant(h, r, func(q *tenantdb.Queries) (tenantdb.MetaobjectEntry, error) {
		return q.GetMetaobjectEntryByID(r.Context(), id)
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, entry)
}

func (h *Handler) UpdateMetaobjectEntry(w http.ResponseWriter, r *http.Request) {
	id, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	var req struct {
		Values map[string]json.RawMessage `json:"values"`
	}
	if err := httpx.Decode(r, &req); err != nil {
		httpx.Error(w, r, err)
		return
	}
	entry, err := inTenant(h, r, func(q *tenantdb.Queries) (tenantdb.MetaobjectEntry, error) {
		existing, err := q.GetMetaobjectEntryByID(r.Context(), id)
		if err != nil {
			return tenantdb.MetaobjectEntry{}, err
		}
		raw, err := validateEntryValues(r, q, existing.DefinitionID, req.Values)
		if err != nil {
			return tenantdb.MetaobjectEntry{}, err
		}
		return q.UpdateMetaobjectEntry(r.Context(), tenantdb.UpdateMetaobjectEntryParams{ID: id, Values: raw})
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, entry)
}

func (h *Handler) DeleteMetaobjectEntry(w http.ResponseWriter, r *http.Request) {
	id, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	rows, err := inTenant(h, r, func(q *tenantdb.Queries) (int64, error) {
		return q.DeleteMetaobjectEntry(r.Context(), id)
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	if rows == 0 {
		httpx.Error(w, r, apperr.NotFound("metaobject entry not found"))
		return
	}
	w.WriteHeader(http.StatusNoContent)
}

// validateEntryValues checks the entry values against the definition's fields
// and returns the marshaled JSONB payload.
func validateEntryValues(r *http.Request, q *tenantdb.Queries, defID uuid.UUID, values map[string]json.RawMessage) (json.RawMessage, error) {
	fields, err := q.ListMetaobjectFields(r.Context(), defID)
	if err != nil {
		return nil, err
	}
	keys := make([]string, len(fields))
	for i, f := range fields {
		keys[i] = f.Key
	}
	if err := validation.ValidateMetaobjectEntry(values, keys); err != nil {
		return nil, err
	}
	if values == nil {
		values = map[string]json.RawMessage{}
	}
	raw, err := json.Marshal(values)
	if err != nil {
		return nil, apperr.Internal(err)
	}
	return raw, nil
}
