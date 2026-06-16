-- name: CreateCategoryAttribute :one
INSERT INTO category_attributes (category_id, attribute_id, required, marketplace_required, sort_order)
VALUES ($1, $2, $3, $4, $5)
RETURNING *;

-- name: GetCategoryAttribute :one
SELECT * FROM category_attributes WHERE id = $1;

-- name: ListCategoryAttributes :many
SELECT * FROM category_attributes WHERE category_id = $1 ORDER BY sort_order;

-- name: UpdateCategoryAttribute :one
UPDATE category_attributes
SET required = $2, marketplace_required = $3, sort_order = $4
WHERE id = $1
RETURNING *;

-- name: DeleteCategoryAttribute :execrows
DELETE FROM category_attributes WHERE id = $1;

-- name: ListCategoryAttributeDefs :many
SELECT
    ca.id            AS category_attribute_id,
    ca.required,
    ca.marketplace_required,
    ca.sort_order,
    a.id             AS attribute_id,
    a.key,
    a.label,
    a.data_type,
    a.value_source,
    a.binding_level,
    a.metaobject_definition_id,
    a.inline_options,
    a.validation
FROM category_attributes ca
JOIN attributes a ON a.id = ca.attribute_id
WHERE ca.category_id = $1
ORDER BY ca.sort_order;
