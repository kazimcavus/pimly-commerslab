-- Tenant template schema (version 4): tenant settings + variant value codes.
-- settings is a small key-value JSONB store (e.g. 'sku', 'barcode' configs) so
-- config shapes can evolve without migrations. variant_values.code holds an
-- optional code used when building structured SKUs (e.g. color "R08").
-- Idempotent (IF NOT EXISTS) so re-application is safe.

CREATE TABLE IF NOT EXISTS settings (
    key        text PRIMARY KEY,
    value      jsonb NOT NULL DEFAULT '{}'::jsonb,
    updated_at timestamptz NOT NULL DEFAULT now()
);

ALTER TABLE variant_values ADD COLUMN IF NOT EXISTS code text;
