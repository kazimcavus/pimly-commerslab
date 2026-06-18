package pimhttp

import (
	"encoding/json"
	"net/http"

	"github.com/kazimcavus/pimly/internal/platform/db/tenantdb"
	"github.com/kazimcavus/pimly/internal/platform/httpx"
	"github.com/kazimcavus/pimly/internal/shared/apperr"
)

// Tenant settings are a small key-value JSONB store (e.g. "sku", "barcode").

// ListSettings returns all settings as a { key: value } object.
func (h *Handler) ListSettings(w http.ResponseWriter, r *http.Request) {
	rows, err := inTenant(h, r, func(q *tenantdb.Queries) ([]tenantdb.Setting, error) {
		return q.ListSettings(r.Context())
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	out := make(map[string]json.RawMessage, len(rows))
	for _, s := range rows {
		out[s.Key] = s.Value
	}
	httpx.JSON(w, http.StatusOK, out)
}

// PutSetting upserts one setting; the request body is the raw JSON value.
func (h *Handler) PutSetting(w http.ResponseWriter, r *http.Request) {
	key := r.PathValue("key")
	if key == "" {
		httpx.Error(w, r, apperr.Validation("key is required"))
		return
	}
	var value json.RawMessage
	if err := httpx.Decode(r, &value); err != nil {
		httpx.Error(w, r, err)
		return
	}
	if len(value) == 0 {
		value = json.RawMessage("{}")
	}
	s, err := inTenant(h, r, func(q *tenantdb.Queries) (tenantdb.Setting, error) {
		return q.UpsertSetting(r.Context(), tenantdb.UpsertSettingParams{Key: key, Value: value})
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, s)
}
