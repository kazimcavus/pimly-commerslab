-- name: CreateMembership :one
INSERT INTO memberships (user_id, tenant_id, role)
VALUES ($1, $2, $3)
RETURNING *;

-- name: ListMembershipsByUser :many
SELECT * FROM memberships WHERE user_id = $1 ORDER BY created_at;

-- name: GetMembershipByUserAndTenant :one
SELECT * FROM memberships WHERE user_id = $1 AND tenant_id = $2;

-- name: GetFirstMembershipForUser :one
SELECT * FROM memberships WHERE user_id = $1 ORDER BY created_at LIMIT 1;
