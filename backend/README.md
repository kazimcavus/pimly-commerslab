# Pimly .NET Backend

Modular monolith ASP.NET Core API with DDD building blocks, the Catalog module (Categories, Attributes, MetaObjects, Variant Types/Values, Products), and the Identity module (JWT auth).

## Prerequisites

- .NET 9 SDK
- PostgreSQL (local Docker Compose from repo root: `docker compose up -d`)

## Quick start

```bash
cd backend

# Apply database migrations
dotnet ef database update \
  --project src/Modules/Catalog/Catalog.Infrastructure \
  --startup-project src/Pimly.Api

dotnet ef database update \
  --project src/Modules/Identity/Identity.Infrastructure \
  --startup-project src/Pimly.Api

# Run API
dotnet run --project src/Pimly.Api
```

API listens on `http://localhost:7000` (HTTPS: `https://localhost:7001`). Swagger UI is enabled in Development at `/swagger`.

Connection strings in `src/Pimly.Api/appsettings.Development.json`:

- `ConnectionStrings:Database` — Catalog schema (`catalog`)
- `ConnectionStrings:Identity` — Identity schema (`identity`)

Default: `Host=localhost;Port=5432;Database=pimly;Username=pimly;Password=pimly`

## Solution layout

```
src/
  SharedKernel/                 Entity, AggregateRoot, ValueObject, DomainEvent, Result
  Pimly.Api/                    HTTP host (composition root)
  Modules/Catalog/
    Catalog.Domain/             Aggregates + repository interfaces
    Catalog.Application/        Use cases (vertical slices) + FluentValidation
    Catalog.Infrastructure/     EF Core + PostgreSQL (schema: catalog)
    Catalog.Api/                Minimal API endpoints + request modelleri
  Modules/Identity/
    Identity.Domain/            User aggregate + repository interfaces
    Identity.Application/       Login, GetMe use cases + FluentValidation
    Identity.Infrastructure/    EF Core + PostgreSQL (schema: identity), JWT, PasswordHasher
    Identity.Api/               Minimal API endpoints
tests/
  Catalog.Domain.UnitTests/
  Catalog.Application.UnitTests/
  Catalog.IntegrationTests/
  Identity.Application.UnitTests/
  Identity.IntegrationTests/
```

## Configuration

| Key | Default | Description |
|-----|---------|-------------|
| `ConnectionStrings:Identity` | same as `Database` | PostgreSQL connection for `identity` schema |
| `Identity:AutoMigrate` | `true` | Apply EF migrations on startup |
| `Identity:Jwt:Secret` | `change-me-in-production` | HS256 signing secret |
| `Identity:Jwt:ExpirationHours` | `24` | Access token lifetime |

## API (v1)

### Identity

Base path: `/api/v1/identity`

| Resource | Endpoints |
|---|---|
| Auth | `POST /login`, `GET /me` (Bearer token required) |

Login request:

```json
{ "email": "user@example.com", "password": "secret" }
```

Login response:

```json
{
  "token": "...",
  "expiresAt": "...",
  "user": { "id": "...", "email": "...", "name": "..." }
}
```

All `/api/v1/catalog/*` endpoints require a valid JWT bearer token. Only `POST /api/v1/identity/login` and `GET /healthz` are public.

### Catalog

Base path: `/api/v1/catalog` — **JWT bearer token required** for all endpoints.

| Resource | Endpoints |
|---|---|
| Categories | `GET/POST /categories`, `GET/PATCH/DELETE /categories/{id}` |
| Category attributes | `POST/GET /categories/{id}/attributes`, `PATCH/DELETE /category-attributes/{id}` |
| Attributes | `GET/POST /attributes`, `GET/PATCH/DELETE /attributes/{id}` |
| Attribute values | `POST/GET /attributes/{id}/values`, `PATCH/DELETE /attribute-values/{id}` |
| Variant types | `GET/POST /variants`, `GET/PATCH/DELETE /variants/{id}` |
| Variant values | `POST/GET /variants/{id}/values`, `PATCH/DELETE /variant-values/{id}` |
| Products | `POST /products`, `POST /products:batch`, `GET/PATCH/DELETE /products/{id}` |
| Product items | `GET/PATCH/DELETE /items/{id}` |

> A "variant type" is an option axis (Renk, Beden) and lives under `/variants`;
> a "product item" is a concrete SKU row under a product and lives under `/items`.
> MetaObjects and marketplace-map endpoints are **not yet implemented** in .NET
> (planned; present in the legacy Go backend).

Health: `GET /healthz`

> **Wire format:** the host serializes JSON as **snake_case** (request + response),
> matching the web client. Single-word and compound property names alike are
> emitted/accepted in snake_case (e.g. `selection_style`, `sort_order`, `parent_id`).

## Tests

```bash
dotnet test backend/tests/Catalog.Domain.UnitTests
dotnet test backend/tests/Catalog.Application.UnitTests
dotnet test backend/tests/Catalog.IntegrationTests
dotnet test backend/tests/Identity.Application.UnitTests
dotnet test backend/tests/Identity.IntegrationTests
```

### Unit tests

Domain and application validator tests run without external dependencies.

### Integration tests

Integration tests spin up an isolated **PostgreSQL Testcontainer** (`postgres:17-alpine`, database `pimly`) via [Testcontainers](https://dotnet.testcontainers.org/). **Docker must be running** on the machine executing the tests.

Migrations are applied automatically when the fixture starts. If Docker is unavailable, integration tests are **skipped** (`Xunit.SkippableFact`) rather than failing the build.

## Notes

- v1 is single-tenant (no schema-per-tenant yet).
- Identity uses ASP.NET `PasswordHasher` and minimal JWT claims (`sub`, `email`). No role-based authorization in v1.
- MetaObject CRUD is available (definitions, fields, entries). Attribute `value_source=metaobject` integration is Phase 2.
- Products v1 accepts `GroupId` as a required FK reference without Group CRUD or category attribute validation.
- Slicer variant types split into multiple products via `POST /products:batch`; `POST /products` creates exactly one product.
