-- name: UpsertMarketplaceCategoryMap :one
INSERT INTO marketplace_category_map (category_id, marketplace, marketplace_category_id, marketplace_category_path)
VALUES ($1, $2, $3, $4)
ON CONFLICT (category_id, marketplace)
DO UPDATE SET marketplace_category_id = EXCLUDED.marketplace_category_id,
              marketplace_category_path = EXCLUDED.marketplace_category_path
RETURNING *;

-- name: ListMarketplaceCategoryMaps :many
SELECT * FROM marketplace_category_map WHERE category_id = $1 ORDER BY marketplace;

-- name: DeleteMarketplaceCategoryMap :execrows
DELETE FROM marketplace_category_map WHERE id = $1;

-- name: UpsertMarketplaceAttributeMap :one
INSERT INTO marketplace_attribute_map (category_id, attribute_id, marketplace, marketplace_attribute_id, marketplace_attribute_name, required)
VALUES ($1, $2, $3, $4, $5, $6)
ON CONFLICT (category_id, attribute_id, marketplace)
DO UPDATE SET marketplace_attribute_id = EXCLUDED.marketplace_attribute_id,
              marketplace_attribute_name = EXCLUDED.marketplace_attribute_name,
              required = EXCLUDED.required
RETURNING *;

-- name: ListMarketplaceAttributeMaps :many
SELECT * FROM marketplace_attribute_map WHERE category_id = $1 ORDER BY marketplace;

-- name: DeleteMarketplaceAttributeMap :execrows
DELETE FROM marketplace_attribute_map WHERE id = $1;
