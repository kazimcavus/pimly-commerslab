-- name: CreateTenant :one
INSERT INTO tenants (name, slug, schema_name, status, barcode_tenant_code, approved_at)
VALUES ($1, $2, $3, $4, $5, $6)
RETURNING *;

-- name: GetTenantBySlug :one
SELECT * FROM tenants WHERE slug = $1;

-- name: GetTenantByID :one
SELECT * FROM tenants WHERE id = $1;

-- name: ListTenants :many
SELECT * FROM tenants ORDER BY created_at;

-- name: ListActiveTenants :many
SELECT * FROM tenants WHERE status = 'active' ORDER BY created_at;

-- name: SetTenantStatus :one
UPDATE tenants SET status = $2, approved_at = $3 WHERE id = $1 RETURNING *;
