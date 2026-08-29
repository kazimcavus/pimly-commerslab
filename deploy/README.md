# Pimly VPS Dağıtım Rehberi

Go backend'in (6 binary: API + 5 worker) sıfırdan bir VPS'e kurulumu.
Bu ilk kurulumdur — ortada devralınacak .NET kurulumu yoktur, dolayısıyla
paralel çalıştırma/geri dönüş adımları gerekmez.

## Ne çalışacak

| Servis | Rol | İç port |
|---|---|---|
| `pimly-go-api` | 88 HTTP ucu, `/media` statik sunumu, şema migration'ları | 7000 |
| `pimly-go-outbox-worker` | Modül olaylarını dağıtır (SKIP LOCKED + backoff + dead-letter) | 7001 |
| `pimly-go-taxonomy-sync-worker` | Pazaryeri kategori ağacını senkronlar (kuyruk + zamanlayıcı) | 7002 |
| `pimly-go-product-imports-worker` | Pazaryerinden ürün içe aktarır (görselleri indirir) | 7003 |
| `pimly-go-listing-sync-worker` | Fiyat/stok ve içerik senkronu **(pazaryerine yazar)** | 7004 |
| `pimly-go-product-publications-worker` | Yeni ürün yayını **(pazaryerine yazar)** | 7005 |
| `postgres` | Veritabanı (tüm şemalar) | 5432 |
| `caddy` | TLS sonlandırma, SPA + API tek origin | 80/443 |

Kaynak beklentisi: 6 Go süreci toplam **~150-250 MB** RAM (GOMEMLIMIT ile
sınırlı), boşta CPU sıfıra yakın, soğuk başlatma <100 ms.

## Ön koşullar

- Docker ve Docker Compose v2 kurulu bir VPS (Ubuntu 22.04+ önerilir)
- Alan adının A kaydı VPS'in IP'sine işaret ediyor
- 80 ve 443 portları dışarı açık (Caddy Let's Encrypt için 80'i kullanır)

## Kurulum

```bash
git clone https://github.com/kazimcavus/pimly-commerslab.git
cd pimly-commerslab

# 1) Ortam değişkenleri
cp .env.prod.example .env.prod
nano .env.prod        # PIMLY_DOMAIN, parolalar, JWT_SECRET

# Güçlü sır üretmek için:
#   openssl rand -base64 32   → POSTGRES_PASSWORD
#   openssl rand -base64 48   → JWT_SECRET

# 2) Frontend'i derle (Caddy web/dist klasörünü servis eder)
cd web && npm ci && npm run build && cd ..

# 3) Ayağa kaldır
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d --build
```

API ilk açılışta tüm şema migration'larını kendisi uygular; elle bir adım
gerekmez.

### İzlemeyle birlikte (opsiyonel)

```bash
docker compose -f docker-compose.prod.yml -f docker-compose.monitoring.yml \
  --env-file .env.prod --profile monitoring up -d --build
```

Prometheus'un 6 servisi de kazıması için `docker-compose.monitoring.yml`
içindeki prometheus volume satırını `prometheus.prod.yml`e çevirin:

```yaml
- ./monitoring/prometheus/prometheus.prod.yml:/etc/prometheus/prometheus.yml:ro
```

Promtail zaten `pimly-api` ve tüm `pimly-go-*` container'larını topluyor
(regex `monitoring/promtail/promtail-config.yml` içinde güncellendi). Worker
logları .NET döneminde hiç görünmüyordu; Go'da ilk kez Loki'ye akar.

## ⚠️ Pazaryerine yazma güvenlik anahtarı

`.env.prod` içindeki **`CHANNELS_USE_STUB_CLIENTS`** dağıtımın en kritik
ayarıdır:

- **`true` (varsayılan, önerilen ilk kurulum):** `listing-sync` ve
  `publications` worker'ları stub istemci kullanır — Trendyol'a **hiçbir**
  fiyat/stok güncellemesi veya ürün kartı gönderilmez. Taksonomi ve ürün
  import'u da stub veriyle çalışır (gerçek mağaza verisi çekilmez).
- **`false`:** Gerçek Trendyol istemcileri devreye girer. Mağazadaki fiyat ve
  stoklar güncellenir, yeni ürün kartları açılır, ürünler yeniden onaya girer.

### Önerilen kademeli açılış

1. `CHANNELS_USE_STUB_CLIENTS=true` ile kur, sistemin ayakta olduğunu doğrula
   (aşağıdaki sağlık kontrolleri).
2. Panelden bir mağaza bağlantısı ekle (seller id + API anahtarı/gizli anahtarı).
3. `false` yap, **yalnızca** taksonomi + ürün import'u çalıştır (bu ikisi
   pazaryerinden sadece **okur**, hiçbir şey yazmaz) ve katalogun doğru
   dolduğunu kontrol et.
4. Yazma akışını açmadan önce: `pricing.channel_prices` tablosuna kasıtlı
   olarak **tek bir ürün** için kanal fiyatı gir ve listing-sync'in yalnızca o
   kalemi gönderdiğini doğrula. (Kanal fiyatı girilmemiş kalemler asla
   gönderilmez — temel fiyat tek başına yayını tetiklemez.)

## Sağlık kontrolleri

```bash
# Container durumları
docker compose -f docker-compose.prod.yml --env-file .env.prod ps

# API dışarıdan
curl -f https://$PIMLY_DOMAIN/healthz

# Servislerin iç sağlık/hazırlık uçları
for p in 7000 7001 7002 7003 7004 7005; do
  docker compose -f docker-compose.prod.yml --env-file .env.prod \
    exec caddy wget -qO- http://pimly-go-api:$p/healthz 2>/dev/null || true
done

# Loglar
docker compose -f docker-compose.prod.yml --env-file .env.prod logs -f pimly-go-api
```

## Güncelleme

```bash
git pull
cd web && npm ci && npm run build && cd ..
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d --build
```

Go servisleri zarif kapanır (`stop_grace_period: 30s`): API önce `/ready`
ucunu 503'e düşürür, açık istekleri 15 saniyelik bütçeyle bitirir; worker'lar
eldeki partiyi tamamlayıp durur. Kuyruklar `FOR UPDATE SKIP LOCKED` ile
korunduğu için yeniden başlatma sırasında iş kaybı ya da çift işleme olmaz.

## Yedekleme

```bash
# Veritabanı
docker compose -f docker-compose.prod.yml --env-file .env.prod \
  exec postgres pg_dump -U pimly pimly | gzip > pimly-$(date +%F).sql.gz

# Medya dosyaları (pimly_media volume'u)
docker run --rm -v pimly_media:/data -v "$PWD":/backup alpine \
  tar czf /backup/pimly-media-$(date +%F).tar.gz -C /data .
```

## Sorun giderme

| Belirti | Bakılacak yer |
|---|---|
| TLS sertifikası alınamıyor | DNS A kaydı doğru mu, 80 portu açık mı (`docker logs pimly-caddy`) |
| API 500 dönüyor | `docker logs pimly-go-api` — migration ya da DB bağlantı hatası |
| Worker iş almıyor | İlgili tablodaki `status='pending'` satırları; worker logunda "claimed" satırı |
| Pazaryerine yazma olmuyor | `CHANNELS_USE_STUB_CLIENTS` değeri; `pricing.channel_prices` dolu mu |
| Grafana'da worker logu yok | Promtail regex'i (`/(pimly-api\|pimly-go-.*)`) ve container adları |
