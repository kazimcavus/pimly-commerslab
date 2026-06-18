-- Tenant template schema (version 2): variant types & values.
-- A variant type is a reusable option axis (Renk, Beden, Ölçü). Each has a
-- selection style — a plain list, or a color/swatch picker — and a set of
-- values. Products select 1–3 variant types and the variant rows are the
-- cartesian product of their chosen values (wired in a later migration).
-- Idempotent (IF NOT EXISTS) so re-application is safe.

CREATE TABLE IF NOT EXISTS variant_types (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name            text NOT NULL UNIQUE,
    selection_style text NOT NULL DEFAULT 'list' CHECK (selection_style IN ('list','color')),
    sort_order      integer NOT NULL DEFAULT 0,
    created_at      timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS variant_values (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    variant_type_id uuid NOT NULL REFERENCES variant_types(id) ON DELETE CASCADE,
    label           text NOT NULL,
    color           text,        -- hex swatch for selection_style='color'
    image_url       text,        -- optional image for selection_style='color'
    sort_order      integer NOT NULL DEFAULT 0,
    created_at      timestamptz NOT NULL DEFAULT now(),
    UNIQUE (variant_type_id, label)
);

CREATE INDEX IF NOT EXISTS idx_variant_values_type ON variant_values(variant_type_id);
