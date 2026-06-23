# pimly — web (yönetim arayüzü)

pimly PIM backend'i için React + Vite yönetim arayüzü. Görsel tasarım, Claude
Design'dan teslim alınan **pimly Tasarım Sistemi**nin birebir uygulamasıdır
(token'lar ve primitive'ler `src/ds/` altında, ekranlar `src/screens/` altında),
canlı .NET API'sine bağlıdır.

> **Backend:** [`../backend`](../backend) altındaki **.NET** API (ASP.NET Core, `:7000`).

## Çalıştırma (yerel geliştirme)

Önce backend (depo kökünden):

```bash
docker compose up -d                                  # postgres

cd backend
DOTNET_ROLL_FORWARD=Major dotnet run --project src/Pimly.Api   # http://localhost:7000
```

Development'ta varsayılan kullanıcı tohumlanır: **`owner@acme.test` / `demo1234`**.

Sonra frontend:

```bash
cd web
npm install
npm run dev        # http://localhost:5173
```

<http://localhost:5173> açıp giriş yapın. Üst bardaki güneş/ay düğmesi açık/koyu
temayı değiştirir.

## Nasıl bağlanır

- `vite.config.js`, `/api/*` isteklerini `:7000`'deki .NET backend'e proxy'ler
  (`PIMLY_API_TARGET` ile değiştirilebilir); `/api/v1/...` öneki olduğu gibi
  iletilir, böylece tarayıcı same-origin kalır (CORS yok).
- `src/lib/api.js` API istemcisidir: versiyonlu modül öneklerini
  (`/api/v1/identity`, `/api/v1/catalog`) hedefler, JWT bearer token gönderir,
  RFC 7807 `ProblemDetails` hata şeklini çözer.
- Tel formatı her iki yönde **snake_case**'tir (.NET host snake_case JSON ile
  yapılandırılmıştır).
- `src/ds/` tasarım sistemi primitive'leri, `src/styles/` tasarım token'ları
  (CSS değişkenleri) + UI-kit CSS'idir.

## Ekranlar

| Ekran | Açıklama | Backend |
|---|---|---|
| Giriş | E-posta + şifre (JWT) | `/api/v1/identity` |
| Panel | .NET ürün verisinden hafif özet | `/catalog/products` |
| Kategoriler | Kategori + kategori-özellik atamaları | `/catalog/categories` |
| Özellikler | Özellik tanımları + değerleri | `/catalog/attributes` |
| Varyantlar | Varyant tipleri (Renk, Beden…) + değerleri | `/catalog/variants` |
| Ürünler | Liste + tekli/varyantlı toplu oluşturma | `/catalog/products:batch` |
| Ayarlar | Ürün kodu üretici + barkod serisi | aşağıya bakın |

### Ayarlar

- **Ürün Kodu (SKU) Oluşturucu** — frontend-only. Yapılandırma tarayıcı
  `localStorage`'ında (`pimly_sku_config`) tutulur; backend yoktur. Segment
  mantığı ileride .NET'e taşınmak üzere
  [`../docs/product-code-generator.md`](../docs/product-code-generator.md)
  içinde belgelenmiştir. (Ortak yardımcı: `src/lib/skuConfig.js`.)
- **Barkod (EAN-13)** — gerçek .NET barkod serisine bağlıdır
  (`/catalog/barcode-sequence`, `/catalog/barcodes:allocate`).

## Build

```bash
npm run build      # → dist/
```
