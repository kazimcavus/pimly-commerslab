# Pimly .NET Backend

DDD yapı taşları üzerine kurulu, modüler monolit bir ASP.NET Core API. İki modül:
**Catalog** (kategoriler, özellikler, varyant tipleri/değerleri, ürünler, ürün
kalemleri, barkod serisi) ve **Identity** (JWT kimlik doğrulama).

## Gereksinimler

- .NET 9 SDK _(kurulu sürüm farklıysa çalıştırırken `DOTNET_ROLL_FORWARD=Major` kullanın)_
- PostgreSQL — depo kökünden: `docker compose up -d`

## Hızlı başlangıç

```bash
cd backend
DOTNET_ROLL_FORWARD=Major dotnet run --project src/Pimly.Api
```

- API `http://localhost:7000` üzerinde dinler (HTTPS: `https://localhost:7001`).
- Migration'lar açılışta **otomatik** uygulanır (`Catalog:AutoMigrate` ve
  `Identity:AutoMigrate` = `true`).
- Development'ta **varsayılan kullanıcı tohumlanır:** `owner@acme.test` / `demo1234`.
- Swagger UI Development'ta `/swagger` altında açıktır.

> EF migration'larını elle uygulamak isterseniz:
> ```bash
> dotnet ef database update --project src/Modules/Catalog/Catalog.Infrastructure  --startup-project src/Pimly.Api
> dotnet ef database update --project src/Modules/Identity/Identity.Infrastructure --startup-project src/Pimly.Api
> ```

Bağlantı dizeleri `src/Pimly.Api/appsettings.json` içinde
(`Host=localhost;Port=5432;Database=pimly;Username=pimly;Password=pimly`).

## Çözüm yapısı

```
src/
  SharedKernel/                 Entity, AggregateRoot, ValueObject, DomainEvent, Result
  Pimly.Api/                    HTTP host (composition root)
  Pimly.AspNetCore/             Ortak ASP.NET Core yardımcıları
  Modules/Catalog/
    Catalog.Domain/             Aggregate'ler + repository arayüzleri
    Catalog.Application/        Use case'ler (dikey dilimler) + FluentValidation
    Catalog.Infrastructure/     EF Core + PostgreSQL (schema: catalog)
    Catalog.Api/                Minimal API endpoint'leri + request modelleri
  Modules/Identity/
    Identity.Domain/            User aggregate + repository arayüzleri
    Identity.Application/       Login, GetMe use case'leri + FluentValidation
    Identity.Infrastructure/    EF Core + PostgreSQL (schema: identity), JWT, PasswordHasher
    Identity.Api/               Minimal API endpoint'leri
tests/
  Catalog.Domain.UnitTests/ · Catalog.Application.UnitTests/ · Catalog.IntegrationTests/
  Identity.Application.UnitTests/ · Identity.IntegrationTests/
```

## Yapılandırma

| Anahtar | Varsayılan | Açıklama |
|---|---|---|
| `ConnectionStrings:Database` | `Host=localhost;...` | Catalog şeması bağlantısı |
| `ConnectionStrings:Identity` | `Database` ile aynı | Identity şeması bağlantısı |
| `Catalog:AutoMigrate` | `true` | Açılışta EF migration uygula |
| `Identity:AutoMigrate` | `true` | Açılışta EF migration uygula |
| `Identity:Jwt:Secret` | `change-me-in-production` | HS256 imzalama anahtarı |
| `Identity:Jwt:ExpirationHours` | `24` | Erişim token'ı ömrü |

## API (v1)

JSON **snake_case** (istek + yanıt). `POST /api/v1/identity/login` ve `GET /healthz`
dışındaki tüm uçlar geçerli bir **JWT bearer token** ister.

### Identity — `/api/v1/identity`

| Kaynak | Uçlar |
|---|---|
| Auth | `POST /login`, `GET /me` (Bearer token) |

Login isteği: `{ "email": "user@example.com", "password": "secret" }`
Login yanıtı: `{ "token", "expires_at", "user": { "id", "email", "name" } }`

### Catalog — `/api/v1/catalog`

| Kaynak | Uçlar |
|---|---|
| Kategoriler | `GET/POST /categories`, `GET/PATCH/DELETE /categories/{id}` |
| Kategori özellikleri | `POST/GET /categories/{id}/attributes`, `PATCH/DELETE /category-attributes/{id}` |
| Özellikler | `GET/POST /attributes`, `GET/PATCH/DELETE /attributes/{id}` |
| Özellik değerleri | `POST/GET /attributes/{id}/values`, `PATCH/DELETE /attribute-values/{id}` |
| Varyant tipleri | `GET/POST /variants`, `GET/PATCH/DELETE /variants/{id}` |
| Varyant değerleri | `POST/GET /variants/{id}/values`, `PATCH/DELETE /variant-values/{id}` |
| Ürünler | `POST /products`, `POST /products:batch`, `GET/PATCH/DELETE /products/{id}` |
| Ürün kalemleri | `GET/PATCH/DELETE /items/{id}` |
| Barkod serisi | `GET/PUT /barcode-sequence`, `POST /barcodes:allocate`, `GET /barcode-allocations` |

Sağlık: `GET /healthz`

> "Varyant tipi" bir seçenek ekseni (Renk, Beden) olup `/variants` altında; "ürün
> kalemi" bir ürünün altındaki somut SKU satırı olup `/items` altında yaşar.

## Testler

```bash
dotnet test tests/Catalog.Domain.UnitTests
dotnet test tests/Catalog.Application.UnitTests
dotnet test tests/Catalog.IntegrationTests        # Docker gerekir (Testcontainers)
dotnet test tests/Identity.Application.UnitTests
dotnet test tests/Identity.IntegrationTests        # Docker gerekir (Testcontainers)
```

Entegrasyon testleri izole bir **PostgreSQL Testcontainer** (`postgres:17-alpine`)
ayağa kaldırır; Docker yoksa testler **atlanır** (`SkippableFact`), build kırılmaz.

## Notlar

- v1 tek kiracılıdır (şema-başına-kiracı yoktur).
- Identity, ASP.NET `PasswordHasher` ve minimal JWT claim'leri (`sub`, `email`)
  kullanır; v1'de rol tabanlı yetkilendirme yoktur.
- `model_code` üretimi henüz backend'de yoktur; client gönderir. Ürün kodu üretici
  mantığı frontend'de (localStorage) yaşar — bkz.
  [`../docs/product-code-generator.md`](../docs/product-code-generator.md).
- Slicer varyant tipleri `POST /products:batch` ile birden çok ürüne bölünür;
  `POST /products` tam olarak bir ürün oluşturur.
