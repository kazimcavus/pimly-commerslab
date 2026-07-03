# pimly

**pimly**, e-ticaret satıcıları için modüler bir **PIM (Ürün Bilgi Yönetimi)**
platformudur. Satıcılar kendi *kanonik* ürün kataloğunu kurar; bu katalog daha
sonra pazaryerlerine (önce Trendyol) eşlenip gönderilir.

> Backend **.NET** (ASP.NET Core, modüler monolit), arayüz **React + Vite**.
> _(Projenin ilk Go backend'i emekliye ayrıldı ve depodan kaldırıldı.)_

## Depo yapısı

```
backend/   .NET API (Pimly.Api) — Identity + Catalog modülleri  → :7000
web/       React + Vite yönetim arayüzü                          → :5173
docs/      Tasarım/mantık belgeleri (ör. ürün kodu üretici)
docker-compose.yml              Yerel PostgreSQL
docker-compose.monitoring.yml   PLGT observability (profile: monitoring)
monitoring/                     Prometheus, Loki, Tempo, Grafana config
```

## Hızlı başlangıç

```bash
# 1) PostgreSQL
docker compose up -d

# 2) Observability stack (opsiyonel — trace/metrik için)
docker compose -f docker-compose.yml -f docker-compose.monitoring.yml --profile monitoring up -d
# Grafana: http://localhost:3001 · Prometheus: http://localhost:9090

# 3) Backend — host'ta, hot reload ile
cd backend
DOTNET_ROLL_FORWARD=Major dotnet watch run --project src/Pimly.Api
# Sağlık: http://localhost:7000/healthz · Readiness: /ready · Swagger: /swagger

# 4) Frontend (React + Vite) — :5173
cd ../web
npm install
npm run dev
```

Tarayıcıda <http://localhost:5173> açıp giriş yapın.

**Geliştirme kullanıcısı (otomatik tohumlanır):** `owner@acme.test` / `demo1234`

## Mimari notlar

- Backend `/api/v1/identity` (JWT auth) ve `/api/v1/catalog` (kategori, özellik,
  varyant, ürün, ürün kalemi, barkod) uçlarını sunar. JSON **snake_case**'tir.
- Frontend `/api` isteklerini Vite proxy ile `:7000`'e yönlendirir (same-origin,
  CORS yok). Bkz. [`web/vite.config.js`](web/vite.config.js).
- **Ürün kodu (SKU) oluşturucu** şu an frontend-only'dir (tarayıcı `localStorage`);
  mantığı ileride .NET'e taşımak için [`docs/product-code-generator.md`](docs/product-code-generator.md)
  içinde belgelenmiştir.

Ayrıntılar: [`backend/README.md`](backend/README.md) · [`web/README.md`](web/README.md)
