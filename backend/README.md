# Pimly .NET Backend

Modular monolith ASP.NET Core API with DDD building blocks and the Catalog module (Categories, Attributes, MetaObjects, Variant Types/Values, Products).

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

# Run API
dotnet run --project src/Pimly.Api
```

API listens on `http://localhost:7000` (HTTPS: `https://localhost:7001`). Swagger UI is enabled in Development at `/swagger`.

Connection string: `ConnectionStrings:Database` in `src/Pimly.Api/appsettings.Development.json` (default: `Host=localhost;Port=5432;Database=pimly;Username=pimly;Password=pimly`).

## Solution layout

```
src/
  SharedKernel/                 Entity, AggregateRoot, ValueObject, DomainEvent, Result
  Pimly.Api/                    HTTP host (composition root)
  Modules/Catalog/
    Catalog.Domain/             Aggregates + repository interfaces
      Categories/
      Attributes/
      MetaObjects/
      Variants/
      Products/                   Product aggregate + ProductVariant entity
    Catalog.Application/        Use cases (vertical slices) + FluentValidation
      Categories/
      Attributes/
      MetaObjects/
      Variants/
      Products/
    Catalog.Infrastructure/     EF Core + PostgreSQL (schema: catalog)
    Catalog.Api/                Minimal API endpoints + request modelleri
tests/
  Catalog.Domain.UnitTests/
  Catalog.Application.UnitTests/
  Catalog.IntegrationTests/
```

## API (v1)

Base path: `/api/v1`

| Resource | Endpoints |
|---|---|
| Categories | `GET/POST /categories`, `GET/PATCH/DELETE /categories/{id}` |
| Category attributes | `POST/GET /categories/{id}/attributes`, `PATCH/DELETE /category-attributes/{id}` |
| Attributes | `GET/POST /attributes`, `GET/PATCH/DELETE /attributes/{id}` |
| MetaObjects | `GET/POST/DELETE /metaobject-definitions`, `GET/POST/DELETE /metaobject-definitions/{id}/fields`, `GET/POST/PATCH/DELETE /metaobject-definitions/{id}/entries`, `DELETE /metaobject-fields/{id}`, `GET/PATCH/DELETE /metaobject-entries/{id}` |
| Variant types | `GET/POST /variant-types`, `GET/PATCH/DELETE /variant-types/{id}` |
| Variant values | `POST/GET /variant-types/{id}/values`, `PATCH/DELETE /variant-values/{id}` |
| Products | `POST /products`, `POST /products:batch`, `GET/PATCH/DELETE /products/{id}` |
| Product variants | `GET/PATCH/DELETE /variants/{id}` |

Health: `GET /healthz`

## Tests

```bash
dotnet test backend/tests/Catalog.Domain.UnitTests
dotnet test backend/tests/Catalog.Application.UnitTests
dotnet test backend/tests/Catalog.IntegrationTests
```

### Unit tests

Domain and application validator tests run without external dependencies.

### Integration tests

Integration tests spin up an isolated **PostgreSQL Testcontainer** (`postgres:17-alpine`, database `pimly`) via [Testcontainers](https://dotnet.testcontainers.org/). **Docker must be running** on the machine executing the tests.

Migrations are applied automatically when the fixture starts. If Docker is unavailable, integration tests are **skipped** (`Xunit.SkippableFact`) rather than failing the build.

## Notes

- v1 is single-tenant (no schema-per-tenant yet).
- MetaObject CRUD is available (definitions, fields, entries). Attribute `value_source=metaobject` integration is Phase 2.
- Products v1 accepts `GroupId` as a required FK reference without Group CRUD or category attribute validation.
- Slicer variant types split into multiple products via `POST /products:batch`; `POST /products` creates exactly one product.
