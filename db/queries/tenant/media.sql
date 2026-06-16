-- name: CreateMedia :one
INSERT INTO media (product_id, variant_id, url, alt_text, sort_order)
VALUES ($1, $2, $3, $4, $5)
RETURNING *;

-- name: ListMediaByProduct :many
SELECT * FROM media WHERE product_id = $1 ORDER BY sort_order, created_at;

-- name: GetMedia :one
SELECT * FROM media WHERE id = $1;

-- name: DeleteMedia :execrows
DELETE FROM media WHERE id = $1;

-- name: NextMediaSortOrder :one
SELECT COALESCE(max(sort_order), -1)::int + 1 AS next FROM media WHERE product_id = $1;
