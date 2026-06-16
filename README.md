# pimly

**pimly** is a modular SaaS **PIM (Product Information Management)** platform for
e-commerce sellers. Sellers build their own *canonical* product catalog, which is
later mapped and pushed to marketplaces (Trendyol first). This repository is the
**core PIM v1 backend** (no UI — the UI is a separate project).

> Status: **v1 complete** (M1–M6). Core PIM: multi-tenant provisioning, auth,
> definition/setup APIs, product core with `products:batch` + code generation,
> media, feature flags, and platform admin. See [Roadmap](#roadmap).

## Architecture at a glance

- **Modular monolith** — a single `pimly` binary; modules are internal packages
  gated by per-tenant feature flags. No microservices.
- **Schema-per-tenant** — every store gets its own PostgreSQL schema
  (`tenant_<slug>`) holding an identical copy of all tenant tables. Access is
  scoped per request via the connection `search_path`.
- **Custom fields are data, not DDL** — user-defined attribute values live in
  JSONB columns (`attribute_values`, `values`, with GIN indexes). Adding an
  attribute never alters a table, so every tenant schema stays structurally
  identical.
- **Single write path** — all product creation flows through one API
  (`POST /products:batch`, M4).
- **Stack**: Go + `net/http` (Go 1.22 ServeMux), PostgreSQL via **pgx v5**,
  type-safe queries via **sqlc**, `slog` logging, env config, MinIO for media.
  No ORM, no Redis.

### Key technical decisions

- **`QueryExecModeExec` (statement cache OFF).** With schema-per-tenant, a pooled
  connection that cached a query plan under one schema and is later reused under
  another (structurally identical) schema triggers PostgreSQL's
  *“cached plan must not change result type”* (`SQLSTATE 0A000`). Exec mode keeps
  the extended protocol (typed/binary params) while skipping statement caching.
  The trade-off — no prepared-statement reuse — is accepted for tenant safety.
- **Tenant scoping via `SET LOCAL search_path` inside a transaction.** `SET LOCAL`
  is transaction-scoped, so connections revert to their default `search_path`
  when returned to the pool — no tenant context leaks across requests. See
  `db.WithTenant`.
- **Two sqlc packages, unqualified table names** — `globaldb` (public schema) and
  `tenantdb` (tenant template). The same generated `tenantdb` code runs against
  any tenant schema selected at runtime.
- **Single-transaction provisioning** — `CreateTenant` does public inserts +
  `CREATE SCHEMA` + template migrations + seed in one transaction; PostgreSQL DDL
  is transactional, so any failure rolls back atomically with no orphans.
- **Per-tenant template migrations** — the global/public schema uses
  golang-migrate; tenant schemas track their own version in a `schema_version`
  table, applied by a small embedded-FS runner shared by provisioning and
  `migrate-tenants`.

## Requirements

- Go 1.26+
- Docker + Docker Compose (PostgreSQL 17, MinIO)
- [`sqlc`](https://sqlc.dev) for regenerating queries:
  `go install github.com/sqlc-dev/sqlc/cmd/sqlc@latest`

## Quick start

```bash
# 1. Start dependencies (Postgres + MinIO)
docker compose up -d

# 2. Build
make build           # -> bin/pimly

# 3. Apply global (public schema) migrations
./bin/pimly migrate

# 4. Provision a tenant (creates schema, tables, seed, owner)
./bin/pimly create-tenant \
  --name "Acme Tekstil" \
  --owner-email owner@acme.test \
  --owner-name "Acme Owner"

# 5. Run the API server
./bin/pimly serve     # listens on :8080 (GET /healthz, /readyz)
```

`create-tenant` prints the new tenant id, schema, owner, and a generated owner
password (store it — it is shown once).

## Configuration

Configuration is read from environment variables; a `.env` file in the working
directory is loaded automatically (without overriding the real environment). See
[`.env.example`](.env.example).

| Variable | Default | Purpose |
|---|---|---|
| `PIMLY_HTTP_ADDR` | `:8080` | HTTP listen address |
| `PIMLY_DATABASE_URL` | `postgres://pimly:pimly@localhost:5432/pimly?sslmode=disable` | Postgres DSN |
| `PIMLY_DB_MAX_CONNS` / `PIMLY_DB_MIN_CONNS` | `8` / `1` | Pool sizing |
| `PIMLY_JWT_SECRET` | _(empty)_ | HS256 signing secret (required for auth) |
| `PIMLY_JWT_TTL` | `24h` | Access token lifetime |
| `PIMLY_ADMIN_TOKEN` | _(empty)_ | Token for `/admin/*` endpoints (empty disables them) |
| `PIMLY_S3_*` | see `.env.example` | MinIO/S3 media storage |
| `PIMLY_LOG_LEVEL` / `PIMLY_LOG_FORMAT` | `info` / `text` | Logging |

## CLI

| Command | Description |
|---|---|
| `pimly serve` | Start the HTTP API server |
| `pimly migrate` | Apply global (public schema) migrations |
| `pimly create-tenant --name --owner-email [--owner-name] [--owner-password]` | Provision a tenant |
| `pimly migrate-tenants [--dry-run] [--tenant <slug>]` | Apply pending template migrations to all tenants |

## Development

```bash
make sqlc              # regenerate globaldb/tenantdb from db/queries + migrations
make test              # unit tests (no Docker)
make test-integration  # integration tests (needs Postgres; uses throwaway DBs)
make vet
```

- **Adding a tenant table/column**: add a new
  `migrations/tenant_template/NNN_*.sql` file, run `make sqlc`, then
  `pimly migrate-tenants` to roll it out to existing tenants.
- **Adding a public table/column**: add a `migrations/global/NNNNNN_*.up.sql` /
  `.down.sql` pair, run `make sqlc`, then `pimly migrate`.
- Integration tests create a fresh throwaway database per test (migrated to the
  latest global schema) and drop it on cleanup. They skip cleanly when no test
  database is reachable. Override the base DB with `PIMLY_TEST_DATABASE_URL`.

## Project layout

```
cmd/pimly                 main.go — wiring + CLI
internal/platform
  config                  env config + logger
  db                      pgx pool, WithTenant (search_path), Tx
  db/globaldb,tenantdb    sqlc-generated query code
  migrate                 global migration runner (golang-migrate)
  tenant                  schema-name validation, slug, context carrier, template runner
  provision               single-transaction tenant provisioning
  auth                    argon2id password hashing (JWT in M2)
  flags                   per-tenant module gating (stub until M6)
internal/modules
  pim                     product catalog (M3–M5)
  admin                   platform admin (M6)
internal/shared
  apperr                  typed errors + HTTP status mapping
  validation, codegen     (M3 / M4)
migrations/global         public schema (golang-migrate)
migrations/tenant_template per-tenant template (programmatic runner)
db/queries                sqlc query sources
```

## Notes & decisions log

Pragmatic choices made during the build (the spec invites reasonable defaults):

- **`provision` is a separate package** (not inside `tenant`) so the low-level
  `db` package can import `tenant` for schema-name validation without an import
  cycle.
- **Error package is `apperr`** (not `errors`) to avoid shadowing the stdlib.
- **Integration tests use throwaway databases on the compose Postgres**, not
  testcontainers — keeps the dependency graph minimal (a spec priority). The spec
  permits either.
- **Internal EAN-13 barcodes** (M4) will use the GS1 restricted-distribution
  prefix band (`29…`) plus a per-tenant code and serial — these are **not
  GS1-registered**; sellers needing real retail EANs supply their own.
- **JWT** uses HS256 with a config secret, behind an interface so the algorithm
  can change.
- **Admin endpoints** are guarded by a static `X-Admin-Token` (config) rather
  than a platform-admin user model, which v1's schema doesn't include. Empty
  token disables `/admin/*` entirely.

## API surface (v1)

All routes below `/login` require a bearer token and are scoped to the caller's
tenant; `/admin/*` routes require the `X-Admin-Token` header.

- **Auth**: `POST /login`, `GET /me`
- **Setup**: `/categories`, `/attributes`, `/metaobject-definitions` (+ `/fields`,
  `/entries`), `/categories/{id}/attributes`, `/categories/{id}/marketplace-*-map`
- **Products**: `POST /products:batch` (single write path), `/groups`,
  `/products/{id}`, `/variants/{id}`
- **Media**: `POST /products/{id}/media`, `POST /media:bulk` (filename = SKU),
  `POST /variants/{id}/media`, `GET /products/{id}/media`
- **Gated**: `GET /integration/status` (requires the `integration` module flag)
- **Admin**: `/admin/applications` (+ `/{id}/approve` → provisions a tenant),
  `/admin/tenants`, `POST /admin/tenants/{id}/modules/{module}`

## Roadmap

- **M1 ✅** Skeleton + DB + provisioning
- **M2 ✅** Auth (login, JWT) + tenant-routing middleware
- **M3 ✅** Setup APIs (categories, attributes, metaobjects, category attributes, marketplace maps)
- **M4 ✅** Product core (groups/products/variants, `products:batch`, SKU/EAN-13 codegen)
- **M5 ✅** Media (MinIO single + bulk-by-SKU upload)
- **M6 ✅** Feature flags enforcement + platform admin (application approval → provisioning)

**Out of scope for v1** (seams are in place): UI, Excel import, MCP ingestion,
the v2 Integration module (Trendyol sync). The single write API, marketplace
mapping tables, and listing/sync tables already exist.
