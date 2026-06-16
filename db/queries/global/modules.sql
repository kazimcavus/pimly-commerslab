-- name: UpsertTenantModule :one
INSERT INTO tenant_modules (tenant_id, module, enabled, enabled_at)
VALUES ($1, $2, $3, $4)
ON CONFLICT (tenant_id, module)
DO UPDATE SET enabled = EXCLUDED.enabled, enabled_at = EXCLUDED.enabled_at
RETURNING *;

-- name: ListTenantModules :many
SELECT * FROM tenant_modules WHERE tenant_id = $1 ORDER BY module;

-- name: GetTenantModule :one
SELECT * FROM tenant_modules WHERE tenant_id = $1 AND module = $2;

-- name: SetTenantModuleEnabled :one
UPDATE tenant_modules
SET enabled = $3, enabled_at = $4
WHERE tenant_id = $1 AND module = $2
RETURNING *;
