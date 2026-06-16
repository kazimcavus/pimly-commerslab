-- name: CreateGroup :one
INSERT INTO groups (group_code, category_id, title, status, attribute_values)
VALUES ($1, $2, $3, $4, $5)
RETURNING *;

-- name: GetGroup :one
SELECT * FROM groups WHERE id = $1;

-- name: GetGroupByCode :one
SELECT * FROM groups WHERE group_code = $1;

-- name: ListGroups :many
SELECT * FROM groups ORDER BY created_at DESC;

-- name: UpdateGroup :one
UPDATE groups
SET title = $2, status = $3, category_id = $4, attribute_values = $5, updated_at = now()
WHERE id = $1
RETURNING *;

-- name: DeleteGroup :execrows
DELETE FROM groups WHERE id = $1;

-- name: GroupCodeExists :one
SELECT EXISTS(SELECT 1 FROM groups WHERE group_code = $1) AS exists;
