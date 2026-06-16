// Package pimstore holds the PIM write-side orchestration: the single
// products:batch write path and attribute-value validation shared with the
// per-entity update handlers.
package pimstore

import (
	"context"
	"encoding/json"

	"github.com/google/uuid"

	"github.com/kazimcavus/pimly/internal/platform/db/tenantdb"
	"github.com/kazimcavus/pimly/internal/shared/apperr"
	"github.com/kazimcavus/pimly/internal/shared/validation"
)

// ValidateAttrs validates an entity's attribute_values for the given binding
// level against the tenant's attribute definitions and the category's required
// flags. enforceRequired is true when the entity is active.
func ValidateAttrs(ctx context.Context, q *tenantdb.Queries, categoryID *uuid.UUID, level string, raw json.RawMessage, enforceRequired bool) error {
	values, err := parseAttrMap(raw)
	if err != nil {
		return err
	}
	attrs, err := q.ListAttributes(ctx)
	if err != nil {
		return apperr.Internal(err)
	}
	byKey := make(map[string]validation.AttrMeta, len(attrs))
	for _, a := range attrs {
		byKey[a.Key] = validation.AttrMeta{BindingLevel: a.BindingLevel}
	}
	var required []string
	if categoryID != nil {
		defs, err := q.ListCategoryAttributeDefs(ctx, *categoryID)
		if err != nil {
			return apperr.Internal(err)
		}
		for _, d := range defs {
			if d.Required && d.BindingLevel == level {
				required = append(required, d.Key)
			}
		}
	}
	return validation.ValidateAttributeValues(level, values, byKey, required, enforceRequired)
}

// parseAttrMap parses a JSONB attribute_values payload into a key→value map.
func parseAttrMap(raw json.RawMessage) (map[string]json.RawMessage, error) {
	if len(raw) == 0 || string(raw) == "null" {
		return map[string]json.RawMessage{}, nil
	}
	var m map[string]json.RawMessage
	if err := json.Unmarshal(raw, &m); err != nil {
		return nil, apperr.Validation("attribute_values must be a JSON object")
	}
	return m, nil
}

// attrsOrEmpty normalizes an attribute_values payload to a non-null JSONB value.
func attrsOrEmpty(raw json.RawMessage) json.RawMessage {
	if len(raw) == 0 || string(raw) == "null" {
		return json.RawMessage("{}")
	}
	return raw
}
