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

-- name: UpdateAttribute :one
UPDATE attributes
SET label = $2, data_type = $3, value_source = $4, metaobject_definition_id = $5,
    inline_options = $6, validation = $7, binding_level = $8, is_global = $9
WHERE id = $1
RETURNING *;

-- name: DeleteAttribute :execrows
DELETE FROM attributes WHERE id = $1;
