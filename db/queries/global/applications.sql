-- name: CreateApplication :one
INSERT INTO applications (email, company_name, status)
VALUES ($1, $2, $3)
RETURNING *;

-- name: GetApplicationByID :one
SELECT * FROM applications WHERE id = $1;

-- name: ListApplications :many
SELECT * FROM applications ORDER BY created_at DESC;

-- name: ListApplicationsByStatus :many
SELECT * FROM applications WHERE status = $1 ORDER BY created_at DESC;

-- name: SetApplicationStatus :one
UPDATE applications
SET status = $2, approved_by = $3
WHERE id = $1
RETURNING *;
