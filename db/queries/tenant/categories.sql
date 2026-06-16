-- name: CreateCategory :one
INSERT INTO categories (parent_id, name, code)
VALUES ($1, $2, $3)
RETURNING *;

-- name: GetCategory :one
SELECT * FROM categories WHERE id = $1;

-- name: ListCategories :many
SELECT * FROM categories ORDER BY created_at;

-- name: UpdateCategory :one
UPDATE categories SET parent_id = $2, name = $3, code = $4 WHERE id = $1 RETURNING *;

-- name: DeleteCategory :execrows
DELETE FROM categories WHERE id = $1;
