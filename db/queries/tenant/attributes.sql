-- name: CreateAttribute :one
INSERT INTO attributes (
    key, label, data_type, value_source, metaobject_definition_id,
    inline_options, validation, binding_level, is_global
) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9)
RETURNING *;

-- name: GetAttributeByID :one
SELECT * FROM attributes WHERE id = $1;

-- name: GetAttributeByKey :one
SELECT * FROM attributes WHERE key = $1;

-- name: ListAttributes :many
SELECT * FROM attributes ORDER BY created_at;
