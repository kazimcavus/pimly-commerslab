// Package validation holds cross-cutting domain validation: attribute definition
// consistency and metaobject entry value checks.
package validation

import (
	"encoding/json"
	"strings"

	"github.com/kazimcavus/pimly/internal/shared/apperr"
)

// Allowed attribute data types.
var dataTypes = map[string]bool{
	"text": true, "number": true, "bool": true, "date": true, "money": true,
	"dimension": true, "color": true, "single_select": true, "multi_select": true,
	"metaobject_ref": true, "metaobject_list": true,
}

// Allowed value sources.
var valueSources = map[string]bool{"none": true, "inline": true, "metaobject": true}

// Allowed binding levels.
var bindingLevels = map[string]bool{"group": true, "product": true, "variant": true}

// AttributeDef captures the fields needed to validate an attribute definition.
type AttributeDef struct {
	DataType                string
	ValueSource             string
	BindingLevel            string
	HasMetaobjectDefinition bool
	InlineOptions           json.RawMessage
}

// ValidateAttribute checks data_type / value_source / binding_level consistency.
func ValidateAttribute(a AttributeDef) error {
	if !dataTypes[a.DataType] {
		return apperr.Validation("invalid data_type %q", a.DataType)
	}
	if !valueSources[a.ValueSource] {
		return apperr.Validation("invalid value_source %q", a.ValueSource)
	}
	if a.BindingLevel != "" && !bindingLevels[a.BindingLevel] {
		return apperr.Validation("invalid binding_level %q", a.BindingLevel)
	}

	switch a.ValueSource {
	case "metaobject":
		if !a.HasMetaobjectDefinition {
			return apperr.Validation("value_source=metaobject requires metaobject_definition_id")
		}
	case "inline":
		if !hasJSON(a.InlineOptions) {
			return apperr.Validation("value_source=inline requires inline_options")
		}
	case "none":
		if a.HasMetaobjectDefinition {
			return apperr.Validation("value_source=none must not set metaobject_definition_id")
		}
	}

	// data_type ↔ value_source coherence.
	switch a.DataType {
	case "metaobject_ref", "metaobject_list":
		if a.ValueSource != "metaobject" {
			return apperr.Validation("data_type %q requires value_source=metaobject", a.DataType)
		}
	case "single_select", "multi_select":
		if a.ValueSource != "inline" && a.ValueSource != "metaobject" {
			return apperr.Validation("data_type %q requires value_source inline or metaobject", a.DataType)
		}
	default:
		if a.ValueSource != "none" {
			return apperr.Validation("data_type %q requires value_source=none", a.DataType)
		}
	}
	return nil
}

// ValidateMetaobjectEntry checks that every key in values is a known field of
// the definition. fieldKeys is the set of valid field keys.
func ValidateMetaobjectEntry(values map[string]json.RawMessage, fieldKeys []string) error {
	allowed := make(map[string]bool, len(fieldKeys))
	for _, k := range fieldKeys {
		allowed[k] = true
	}
	for k := range values {
		if !allowed[k] {
			return apperr.Validation("unknown field %q for this metaobject definition", k)
		}
	}
	return nil
}

// AttrMeta describes a tenant attribute for value validation.
type AttrMeta struct {
	BindingLevel string
}

// ValidateAttributeValues checks that every key in values is a known attribute
// bound at the given level. When enforceRequired is true (e.g. transitioning to
// active), each key in requiredKeys must be present and non-empty; draft is
// lenient about required attributes.
func ValidateAttributeValues(level string, values map[string]json.RawMessage, attrsByKey map[string]AttrMeta, requiredKeys []string, enforceRequired bool) error {
	for k := range values {
		meta, ok := attrsByKey[k]
		if !ok {
			return apperr.Validation("unknown attribute %q", k)
		}
		if meta.BindingLevel != level {
			return apperr.Validation("attribute %q is bound at %q, not %q", k, meta.BindingLevel, level)
		}
	}
	if enforceRequired {
		for _, rk := range requiredKeys {
			val, ok := values[rk]
			if !ok || isEmptyValue(val) {
				return apperr.Validation("required attribute %q is missing", rk)
			}
		}
	}
	return nil
}

func isEmptyValue(raw json.RawMessage) bool {
	s := strings.TrimSpace(string(raw))
	return s == "" || s == "null" || s == `""`
}

func hasJSON(raw json.RawMessage) bool {
	if len(raw) == 0 {
		return false
	}
	s := string(raw)
	return s != "null" && s != "{}" && s != "[]" && s != `""`
}
