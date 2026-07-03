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
  Modules/Media/
    Media.Application/          UploadImage use case + IBlobStorage arayüzü
    Media.Infrastructure/       LocalBlobStorage, magic-byte MIME tespiti
    Media.Api/                  Multipart upload endpoint'leri
tests/
  Catalog.Domain.UnitTests/ · Catalog.Application.UnitTests/ · Catalog.IntegrationTests/
  Identity.Application.UnitTests/ · Identity.IntegrationTests/
  Media.Application.UnitTests/
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
| `Media:StoragePath` | `./storage/media` | Yerel blob depolama kök dizini |
| `Media:PublicBaseUrl` | `""` | Dönen URL öneki; boşsa `/media/...` relative path |
| `Media:AllowedUrlPrefix` | `/media/` | Catalog'da kabul edilen görsel URL öneki |
| `Observability:Enabled` | `true` | PLGT observability (Serilog, OTel, health checks) |
| `Observability:Tracing:OtlpEndpoint` | `http://localhost:4317` | Tempo OTLP gRPC (host API + Docker Tempo) |
| `Observability:Tracing:SamplingRatio` | `1.0` | Trace sampling oranı (0.0–1.0) |
| `Observability:Tracing:IncludeSqlStatements` | `false` | EF span'lerine SQL ekle (dev'de `true`) |

## Observability (PLGT)

Self-hosted **Prometheus + Loki + Grafana + Tempo** stack'i. API tarafında
OpenTelemetry metrik/trace, Serilog JSON log ve readiness health check'ler
[`Pimly.AspNetCore/Observability/`](src/Pimly.AspNetCore/Observability/) içinde
merkezileştirilmiştir.

### Observability stack (PLGT)

Self-hosted **Prometheus + Loki + Grafana + Tempo**. API **host'ta** `dotnet run`
/ `dotnet watch` ile çalışır; Docker yalnızca izleme altyapısını sağlar.

Depo kökünden:

```bash
docker compose -f docker-compose.yml -f docker-compose.monitoring.yml --profile monitoring up -d
cd backend && dotnet watch run --project src/Pimly.Api
```

| Servis | URL | Not |
|---|---|---|
| API | http://localhost:7000 | Host'ta `dotnet run` |
| Grafana | http://localhost:3001 | `admin` / `GRAFANA_ADMIN_PASSWORD` (varsayılan: `changeme`) |
| Prometheus | http://localhost:9090 | `host.docker.internal:7000/metrics` scrape |
| Tempo | localhost:4317 (OTLP) | Trace — `appsettings.Development.json` |

**Loglar:** API Docker container (`pimly-api`) içindeyse Promtail stdout'u Loki'ye
gönderir → Grafana **Explore → Loki** veya dashboard **API logs** paneli.
Host'ta `dotnet run` ile çalışıyorsa loglar terminalde kalır (Loki'ye gitmez).
Terminal: `docker logs -f pimly-api`.

**Hata ayıklama (tüm API):** 4xx/5xx yanıtlarında `trace_id` alanı ve `X-Trace-Id`
header döner. Loki'de `Pimly.Api.RequestFailure` logları `ErrorCode`, `ValidationFields`
(alan:kod, body değil) ve `UserId` içerir. Tempo'da aynı `trace_id` ile span zinciri
izlenir.

Opsiyonel env: `GRAFANA_ADMIN_PASSWORD` (`.env` dosyası).

### Health ve metrik uçları

| Uç | Amaç |
|---|---|
| `GET /healthz` | Liveness — process ayakta mı |
| `GET /ready` | Readiness — Catalog DB, Identity DB, media storage |
| `GET /metrics` | Prometheus scrape (production'da internal only) |

Entegrasyon testlerinde `Observability:Enabled=false` ile observability kapatılır.

Grafana Explore → Tempo → service `pimly-api` → trace seç → **Logs for this span**.
Tüm yanıtlarda `X-Trace-Id` header ve hata gövdelerinde `trace_id` alanı vardır.

Community dashboard import: Grafana UI → Dashboards → Import (ör. OpenTelemetry
ASP.NET Core dashboard'ları).

Provisioning dashboard: **Pimly → Pimly API Overview** (`pimly-api-overview.json`).
Grafana yeniden başlatılınca veya ~10 sn içinde görünür:
`docker compose -f docker-compose.yml -f docker-compose.monitoring.yml restart grafana`

Production container image: [`Dockerfile`](Dockerfile) (compose'da API servisi yok).

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
| Ürün görselleri | `POST /products/{id}/images`, `PATCH/DELETE /product-images/{id}` |
| Ürün kalemleri | `GET/PATCH/DELETE /items/{id}` |
| Barkod serisi | `GET/PUT /barcode-sequence`, `POST /barcodes:allocate`, `GET /barcode-allocations` |
| SKU oluşturucu | `GET/PUT /sku-config` |

### Media — `/api/v1/media`

| Kaynak | Uçlar |
|---|---|
| Yükleme | `POST /uploads?purpose=product\|swatch` (multipart, field: `file`) |

Yanıt: `{ "url", "content_type", "size_bytes" }`. Görseller `GET /media/...` üzerinden
statik dosya olarak servis edilir (auth gerekmez).

Sağlık: `GET /healthz` (liveness) · `GET /ready` (readiness: DB + media storage)

> "Varyant tipi" bir seçenek ekseni (Renk, Beden) olup `/variants` altında; "ürün
> kalemi" bir ürünün altındaki somut SKU satırı olup `/items` altında yaşar.

## Testler

```bash
dotnet test tests/Catalog.Domain.UnitTests
dotnet test tests/Catalog.Application.UnitTests
dotnet test tests/Catalog.IntegrationTests        # Docker gerekir (Testcontainers)
dotnet test tests/Identity.Application.UnitTests
dotnet test tests/Identity.IntegrationTests        # Docker gerekir (Testcontainers)
dotnet test tests/Media.Application.UnitTests
```

Entegrasyon testleri izole bir **PostgreSQL Testcontainer** (`postgres:17-alpine`)
ayağa kaldırır; Docker yoksa testler **atlanır** (`SkippableFact`), build kırılmaz.

## Notlar

- v1 tek kiracılıdır (şema-başına-kiracı yoktur).
- Identity, ASP.NET `PasswordHasher` ve minimal JWT claim'leri (`sub`, `email`)
  kullanır; v1'de rol tabanlı yetkilendirme yoktur.
- `model_code` / varyant `sku` üretimi generator açıkken sunucuda yapılır; client
  `code_inputs` gönderir. Kapalıyken client `model_code` gönderir. Bkz.
  [`../docs/product-code-generator.md`](../docs/product-code-generator.md).
- Slicer varyant tipleri `POST /products:batch` ile birden çok ürüne bölünür;
  `POST /products` tam olarak bir ürün oluşturur.
