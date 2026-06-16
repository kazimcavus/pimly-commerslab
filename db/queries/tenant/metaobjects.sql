-- name: CreateMetaobjectDefinition :one
INSERT INTO metaobject_definitions (key, label)
VALUES ($1, $2)
RETURNING *;

-- name: GetMetaobjectDefinitionByKey :one
SELECT * FROM metaobject_definitions WHERE key = $1;

-- name: GetMetaobjectDefinitionByID :one
SELECT * FROM metaobject_definitions WHERE id = $1;

-- name: ListMetaobjectDefinitions :many
SELECT * FROM metaobject_definitions ORDER BY created_at;

-- name: CreateMetaobjectField :one
INSERT INTO metaobject_fields (definition_id, key, label, data_type)
VALUES ($1, $2, $3, $4)
RETURNING *;

-- name: ListMetaobjectFields :many
SELECT * FROM metaobject_fields WHERE definition_id = $1 ORDER BY created_at;

-- name: CreateMetaobjectEntry :one
INSERT INTO metaobject_entries (definition_id, values)
VALUES ($1, $2)
RETURNING *;

-- name: GetMetaobjectEntryByID :one
SELECT * FROM metaobject_entries WHERE id = $1;

-- name: ListMetaobjectEntries :many
SELECT * FROM metaobject_entries WHERE definition_id = $1 ORDER BY created_at;
