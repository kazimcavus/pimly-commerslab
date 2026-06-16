-- name: CreateVariant :one
INSERT INTO variants (
    product_id, barcode, gtin, mpn, axis_value_entry_id, axis_value,
    price, compare_at_price, stock, attribute_values
) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10)
RETURNING *;

-- name: GetVariant :one
SELECT * FROM variants WHERE id = $1;

-- name: ListVariantsByProduct :many
SELECT * FROM variants WHERE product_id = $1 ORDER BY created_at;

-- name: UpdateVariant :one
UPDATE variants
SET gtin = $2, mpn = $3, axis_value_entry_id = $4, axis_value = $5,
    price = $6, compare_at_price = $7, stock = $8, attribute_values = $9, updated_at = now()
WHERE id = $1
RETURNING *;

-- name: DeleteVariant :execrows
DELETE FROM variants WHERE id = $1;

-- name: VariantBarcodeExists :one
SELECT EXISTS(SELECT 1 FROM variants WHERE barcode = $1) AS exists;

-- name: NextBarcodeSerial :one
SELECT nextval('barcode_serial')::bigint AS serial;
