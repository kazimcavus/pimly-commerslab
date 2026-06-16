-- Tenant template schema (version 1). Applied programmatically into each tenant
-- schema by the template runner under SET LOCAL search_path = <tenant>, public.
-- Tables are intentionally identical across every tenant; custom attribute values
-- live in JSONB (attribute_values / values), never as added columns.
-- Idempotent (IF NOT EXISTS) so re-application is safe.

-- Per-tenant serial feeding the 6-digit body of generated EAN-13 barcodes.
CREATE SEQUENCE IF NOT EXISTS barcode_serial START 1;

CREATE TABLE IF NOT EXISTS categories (
    id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    parent_id  uuid REFERENCES categories(id) ON DELETE SET NULL,
    name       text NOT NULL,
    code       text,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS metaobject_definitions (
    id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    key        text NOT NULL UNIQUE,
    label      text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS metaobject_fields (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    definition_id uuid NOT NULL REFERENCES metaobject_definitions(id) ON DELETE CASCADE,
    key           text NOT NULL,
    label         text NOT NULL,
    data_type     text NOT NULL,
    created_at    timestamptz NOT NULL DEFAULT now(),
    UNIQUE (definition_id, key)
);

CREATE TABLE IF NOT EXISTS metaobject_entries (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    definition_id uuid NOT NULL REFERENCES metaobject_definitions(id) ON DELETE CASCADE,
    values        jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at    timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS metaobject_entry_map (
    id                      uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    entry_id                uuid NOT NULL REFERENCES metaobject_entries(id) ON DELETE CASCADE,
    marketplace             text NOT NULL,
    marketplace_value_id    text,
    marketplace_value_label text,
    UNIQUE (entry_id, marketplace)
);

CREATE TABLE IF NOT EXISTS attributes (
    id                       uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    key                      text NOT NULL UNIQUE,
    label                    text NOT NULL,
    data_type                text NOT NULL CHECK (data_type IN (
                                 'text','number','bool','date','money','dimension','color',
                                 'single_select','multi_select','metaobject_ref','metaobject_list')),
    value_source             text NOT NULL DEFAULT 'none' CHECK (value_source IN ('none','inline','metaobject')),
    metaobject_definition_id uuid REFERENCES metaobject_definitions(id) ON DELETE SET NULL,
    inline_options           jsonb,
    validation               jsonb,
    binding_level            text NOT NULL DEFAULT 'product' CHECK (binding_level IN ('group','product','variant')),
    is_global                boolean NOT NULL DEFAULT false,
    created_at               timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS category_attributes (
    id                   uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    category_id          uuid NOT NULL REFERENCES categories(id) ON DELETE CASCADE,
    attribute_id         uuid NOT NULL REFERENCES attributes(id) ON DELETE CASCADE,
    required             boolean NOT NULL DEFAULT false,
    marketplace_required boolean NOT NULL DEFAULT false,
    sort_order           integer NOT NULL DEFAULT 0,
    UNIQUE (category_id, attribute_id)
);

CREATE TABLE IF NOT EXISTS groups (
    id               uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    group_code       text NOT NULL UNIQUE,
    category_id      uuid REFERENCES categories(id) ON DELETE SET NULL,
    title            text NOT NULL DEFAULT '',
    status           text NOT NULL DEFAULT 'draft' CHECK (status IN ('draft','active','archived')),
    attribute_values jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at       timestamptz NOT NULL DEFAULT now(),
    updated_at       timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS products (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    group_id               uuid NOT NULL REFERENCES groups(id) ON DELETE CASCADE,
    product_sku            text NOT NULL UNIQUE,
    grouping_value_entry_id uuid REFERENCES metaobject_entries(id) ON DELETE SET NULL,
    title                  text NOT NULL DEFAULT '',
    attribute_values       jsonb NOT NULL DEFAULT '{}'::jsonb,
    status                 text NOT NULL DEFAULT 'draft' CHECK (status IN ('draft','active','archived')),
    created_at             timestamptz NOT NULL DEFAULT now(),
    updated_at             timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS variants (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id         uuid NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    barcode            text NOT NULL UNIQUE,
    gtin               text,
    mpn                text,
    axis_value_entry_id uuid REFERENCES metaobject_entries(id) ON DELETE SET NULL,
    axis_value         text,
    price              numeric(14,2) NOT NULL DEFAULT 0,
    compare_at_price   numeric(14,2),
    stock              integer NOT NULL DEFAULT 0,
    attribute_values   jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at         timestamptz NOT NULL DEFAULT now(),
    updated_at         timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS media (
    id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    variant_id uuid REFERENCES variants(id) ON DELETE CASCADE,
    url        text NOT NULL,
    alt_text   text NOT NULL DEFAULT '',
    sort_order integer NOT NULL DEFAULT 0,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS marketplace_category_map (
    id                        uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    category_id               uuid NOT NULL REFERENCES categories(id) ON DELETE CASCADE,
    marketplace               text NOT NULL,
    marketplace_category_id   text,
    marketplace_category_path text,
    UNIQUE (category_id, marketplace)
);

CREATE TABLE IF NOT EXISTS marketplace_attribute_map (
    id                         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    category_id                uuid NOT NULL REFERENCES categories(id) ON DELETE CASCADE,
    attribute_id               uuid NOT NULL REFERENCES attributes(id) ON DELETE CASCADE,
    marketplace                text NOT NULL,
    marketplace_attribute_id   text,
    marketplace_attribute_name text,
    required                   boolean NOT NULL DEFAULT false,
    UNIQUE (category_id, attribute_id, marketplace)
);

CREATE TABLE IF NOT EXISTS marketplace_listings (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id            uuid NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    marketplace           text NOT NULL,
    marketplace_product_id text,
    status                text NOT NULL DEFAULT 'pending',
    last_synced_at        timestamptz,
    payload               jsonb,
    error                 jsonb,
    UNIQUE (product_id, marketplace)
);

-- GIN indexes on the flexible JSONB value columns.
CREATE INDEX IF NOT EXISTS idx_groups_attrs   ON groups   USING gin (attribute_values);
CREATE INDEX IF NOT EXISTS idx_products_attrs ON products USING gin (attribute_values);
CREATE INDEX IF NOT EXISTS idx_variants_attrs ON variants USING gin (attribute_values);
CREATE INDEX IF NOT EXISTS idx_metaentries_values ON metaobject_entries USING gin (values);

-- Helpful btree indexes for FK traversal.
CREATE INDEX IF NOT EXISTS idx_products_group   ON products(group_id);
CREATE INDEX IF NOT EXISTS idx_variants_product ON variants(product_id);
CREATE INDEX IF NOT EXISTS idx_media_product    ON media(product_id);
CREATE INDEX IF NOT EXISTS idx_media_variant    ON media(variant_id);
CREATE INDEX IF NOT EXISTS idx_catattrs_category ON category_attributes(category_id);
CREATE INDEX IF NOT EXISTS idx_metafields_def    ON metaobject_fields(definition_id);
CREATE INDEX IF NOT EXISTS idx_metaentries_def   ON metaobject_entries(definition_id);
