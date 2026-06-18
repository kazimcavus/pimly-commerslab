-- Tenant template schema (version 3): flat variant model.
-- A product selects 1–3 variant types; its variants are the cartesian product
-- of the chosen values. Each variant carries its own SKU and the option
-- combination it represents. A basic product has no variant types and exactly
-- one variant holding the single SKU/barcode.
-- Idempotent (IF NOT EXISTS) so re-application is safe.

-- Ordered list of the product's chosen variant types: [{id, name, selection_style}].
ALTER TABLE products ADD COLUMN IF NOT EXISTS variant_types jsonb NOT NULL DEFAULT '[]'::jsonb;

-- The option combination for a variant: [{type_id, type_name, value_id, value_label, color, image_url}].
ALTER TABLE variants ADD COLUMN IF NOT EXISTS options jsonb NOT NULL DEFAULT '[]'::jsonb;

-- Per-variant SKU (product_sku stays the base/group code). Unique where present.
ALTER TABLE variants ADD COLUMN IF NOT EXISTS sku text;
CREATE UNIQUE INDEX IF NOT EXISTS uq_variants_sku ON variants(sku) WHERE sku IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_variants_options ON variants USING gin (options);
