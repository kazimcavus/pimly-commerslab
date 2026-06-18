-- name: CreateVariantType :one
INSERT INTO variant_types (name, selection_style, sort_order)
VALUES ($1, $2, $3)
RETURNING *;

-- name: ListVariantTypes :many
SELECT * FROM variant_types ORDER BY sort_order, created_at;

-- name: GetVariantTypeByID :one
SELECT * FROM variant_types WHERE id = $1;

-- name: UpdateVariantType :one
UPDATE variant_types SET name = $2, selection_style = $3, sort_order = $4 WHERE id = $1
RETURNING *;

-- name: DeleteVariantType :execrows
DELETE FROM variant_types WHERE id = $1;

-- name: CreateVariantValue :one
INSERT INTO variant_values (variant_type_id, label, color, image_url, code, sort_order)
VALUES ($1, $2, $3, $4, $5, $6)
RETURNING *;

-- name: ListVariantValues :many
SELECT * FROM variant_values WHERE variant_type_id = $1 ORDER BY sort_order, created_at;

-- name: ListAllVariantValues :many
SELECT * FROM variant_values ORDER BY variant_type_id, sort_order, created_at;

-- name: GetVariantValueByID :one
SELECT * FROM variant_values WHERE id = $1;

-- name: UpdateVariantValue :one
UPDATE variant_values SET label = $2, color = $3, image_url = $4, code = $5, sort_order = $6 WHERE id = $1
RETURNING *;

-- name: DeleteVariantValue :execrows
DELETE FROM variant_values WHERE id = $1;
