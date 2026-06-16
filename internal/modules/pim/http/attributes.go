package pimhttp

import (
	"encoding/json"
	"net/http"

	"github.com/kazimcavus/pimly/internal/platform/db/tenantdb"
	"github.com/kazimcavus/pimly/internal/platform/httpx"
	"github.com/kazimcavus/pimly/internal/shared/apperr"
	"github.com/kazimcavus/pimly/internal/shared/validation"
)

type attributeRequest struct {
	Key                    string          `json:"key"`
	Label                  string          `json:"label"`
	DataType               string          `json:"data_type"`
	ValueSource            string          `json:"value_source"`
	MetaobjectDefinitionID *string         `json:"metaobject_definition_id"`
	InlineOptions          json.RawMessage `json:"inline_options"`
	Validation             json.RawMessage `json:"validation"`
	BindingLevel           string          `json:"binding_level"`
	IsGlobal               bool            `json:"is_global"`
}

func (h *Handler) CreateAttribute(w http.ResponseWriter, r *http.Request) {
	var req attributeRequest
	if err := httpx.Decode(r, &req); err != nil {
		httpx.Error(w, r, err)
		return
	}
	if req.Key == "" || req.Label == "" || req.DataType == "" {
		httpx.Error(w, r, apperr.Validation("key, label and data_type are required"))
		return
	}
	if req.ValueSource == "" {
		req.ValueSource = "none"
	}
	if req.BindingLevel == "" {
		req.BindingLevel = "product"
	}
	mdef, err := optUUID(req.MetaobjectDefinitionID, "metaobject_definition_id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	if err := validation.ValidateAttribute(validation.AttributeDef{
		DataType: req.DataType, ValueSource: req.ValueSource, BindingLevel: req.BindingLevel,
		HasMetaobjectDefinition: mdef != nil, InlineOptions: req.InlineOptions,
	}); err != nil {
		httpx.Error(w, r, err)
		return
	}
	attr, err := inTenant(h, r, func(q *tenantdb.Queries) (tenantdb.Attribute, error) {
		return q.CreateAttribute(r.Context(), tenantdb.CreateAttributeParams{
			Key: req.Key, Label: req.Label, DataType: req.DataType, ValueSource: req.ValueSource,
			MetaobjectDefinitionID: mdef, InlineOptions: req.InlineOptions, Validation: req.Validation,
			BindingLevel: req.BindingLevel, IsGlobal: req.IsGlobal,
		})
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusCreated, attr)
}

func (h *Handler) ListAttributes(w http.ResponseWriter, r *http.Request) {
	attrs, err := inTenant(h, r, func(q *tenantdb.Queries) ([]tenantdb.Attribute, error) {
		return q.ListAttributes(r.Context())
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, attrs)
}

func (h *Handler) GetAttribute(w http.ResponseWriter, r *http.Request) {
	id, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	attr, err := inTenant(h, r, func(q *tenantdb.Queries) (tenantdb.Attribute, error) {
		return q.GetAttributeByID(r.Context(), id)
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, attr)
}

func (h *Handler) UpdateAttribute(w http.ResponseWriter, r *http.Request) {
	id, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	var req attributeRequest
	if err := httpx.Decode(r, &req); err != nil {
		httpx.Error(w, r, err)
		return
	}
	if req.Label == "" || req.DataType == "" {
		httpx.Error(w, r, apperr.Validation("label and data_type are required"))
		return
	}
	if req.ValueSource == "" {
		req.ValueSource = "none"
	}
	if req.BindingLevel == "" {
		req.BindingLevel = "product"
	}
	mdef, err := optUUID(req.MetaobjectDefinitionID, "metaobject_definition_id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	if err := validation.ValidateAttribute(validation.AttributeDef{
		DataType: req.DataType, ValueSource: req.ValueSource, BindingLevel: req.BindingLevel,
		HasMetaobjectDefinition: mdef != nil, InlineOptions: req.InlineOptions,
	}); err != nil {
		httpx.Error(w, r, err)
		return
	}
	attr, err := inTenant(h, r, func(q *tenantdb.Queries) (tenantdb.Attribute, error) {
		return q.UpdateAttribute(r.Context(), tenantdb.UpdateAttributeParams{
			ID: id, Label: req.Label, DataType: req.DataType, ValueSource: req.ValueSource,
			MetaobjectDefinitionID: mdef, InlineOptions: req.InlineOptions, Validation: req.Validation,
			BindingLevel: req.BindingLevel, IsGlobal: req.IsGlobal,
		})
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, attr)
}

func (h *Handler) DeleteAttribute(w http.ResponseWriter, r *http.Request) {
	id, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	rows, err := inTenant(h, r, func(q *tenantdb.Queries) (int64, error) {
		return q.DeleteAttribute(r.Context(), id)
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	if rows == 0 {
		httpx.Error(w, r, apperr.NotFound("attribute not found"))
		return
	}
	w.WriteHeader(http.StatusNoContent)
}
