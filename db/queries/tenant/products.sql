-- name: CreateProduct :one
INSERT INTO products (group_id, product_sku, grouping_value_entry_id, title, attribute_values, status)
VALUES ($1, $2, $3, $4, $5, $6)
RETURNING *;

-- name: GetProduct :one
SELECT * FROM products WHERE id = $1;

-- name: GetProductBySku :one
SELECT * FROM products WHERE product_sku = $1;

-- name: ListProductsByGroup :many
SELECT * FROM products WHERE group_id = $1 ORDER BY created_at;

-- name: UpdateProduct :one
UPDATE products
SET title = $2, status = $3, attribute_values = $4, grouping_value_entry_id = $5, updated_at = now()
WHERE id = $1
RETURNING *;

-- name: DeleteProduct :execrows
DELETE FROM products WHERE id = $1;

-- name: ProductSkuExists :one
SELECT EXISTS(SELECT 1 FROM products WHERE product_sku = $1) AS exists;
