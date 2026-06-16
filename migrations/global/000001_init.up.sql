-- Global (public) schema. Applied by golang-migrate.
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- Mints the per-tenant 4-digit code embedded in internal EAN-13 barcodes (see codegen).
CREATE SEQUENCE IF NOT EXISTS tenant_barcode_code_seq START 1;

CREATE TABLE tenants (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name                text NOT NULL,
    slug                text NOT NULL UNIQUE,
    schema_name         text NOT NULL UNIQUE,
    status              text NOT NULL DEFAULT 'active' CHECK (status IN ('pending','active','suspended')),
    barcode_tenant_code integer NOT NULL UNIQUE,
    created_at          timestamptz NOT NULL DEFAULT now(),
    approved_at         timestamptz
);

CREATE TABLE users (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    email         text NOT NULL UNIQUE,
    password_hash text NOT NULL,
    name          text NOT NULL DEFAULT '',
    created_at    timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE memberships (
    id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id    uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    tenant_id  uuid NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    role       text NOT NULL DEFAULT 'owner' CHECK (role IN ('owner','staff','readonly')),
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (user_id, tenant_id)
);

CREATE TABLE tenant_modules (
    id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id  uuid NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    module     text NOT NULL CHECK (module IN ('pim','integration','wms')),
    enabled    boolean NOT NULL DEFAULT false,
    enabled_at timestamptz,
    UNIQUE (tenant_id, module)
);

CREATE TABLE applications (
    id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    email        text NOT NULL,
    company_name text NOT NULL,
    status       text NOT NULL DEFAULT 'pending' CHECK (status IN ('pending','approved','rejected')),
    created_at   timestamptz NOT NULL DEFAULT now(),
    approved_by  uuid REFERENCES users(id)
);

CREATE INDEX idx_memberships_user ON memberships(user_id);
CREATE INDEX idx_memberships_tenant ON memberships(tenant_id);
CREATE INDEX idx_tenant_modules_tenant ON tenant_modules(tenant_id);
CREATE INDEX idx_applications_status ON applications(status);
