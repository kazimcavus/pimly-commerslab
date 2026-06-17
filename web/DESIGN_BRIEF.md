# pimly — UI Tasarım Brief'i (Claude Design için)

Bu doküman, **pimly** PIM platformunun yönetim arayüzünü tasarlarken kullanılır.
Backend hazır ve API uçları sabittir; tasarım bu uçlara birebir oturmalı. claude.ai
Design'da yeni bir tasarım başlatırken bu brief'i bağlam olarak ver.

## 1. Ürün ne yapıyor?

pimly, e-ticaret satıcıları için bir **PIM (ürün bilgi yönetimi)** platformu.
Kullanıcı kendi **kanonik** ürün kataloğunu kurar (kategoriler, özellikler/attribute,
metaobject'ler → ürünler/varyantlar), sonra pazaryerlerine eşler. Arayüz bir
**B2B admin paneli**: yoğun veri tabloları, formlar, ağaç yapılar. Tüketici dükkânı
değil. Dil: **Türkçe** etiketler (veri Türkçe), teknik terimler İngilizce kalabilir.

## 2. Mimari kısıtlar (tasarımı etkiler)

- **Multi-tenant**: kullanıcı login olunca bir tenant'a (mağaza) scope'lanır. Üst
  barda aktif tenant adı görünmeli.
- **Auth**: `POST /login` → JWT bearer token. Tüm korumalı isteklerde
  `Authorization: Bearer <token>`. 401 → login'e at.
- **Roller**: `owner` (tam), `staff`, `readonly` (placeholder). Tasarımda salt-okunur
  durumu öngör (readonly'de butonlar gizli/pasif).
- **Hata formatı**: tüm hatalar `{ "error": { "code": "...", "message": "..." } }`.
  `code`: validation | not_found | conflict | unauthorized | forbidden | internal.
  Tasarımda form alan hataları + toast/banner için bu yapıyı kullan.
- **Tek yazma yolu**: ürün ağacı **tek formdan** (`products:batch`) kaydedilir —
  grup + ürünler + varyantlar aynı anda. Bu yüzden "Ürün Oluştur" ekranı çok-seviyeli
  bir builder olmalı (aşağıda).
- **Esnek attribute'lar**: ürün/grup/varyant'ın `attribute_values` alanı dinamik —
  hangi alanların gösterileceği kategorinin atanmış attribute'larına ve
  `binding_level`'a (group/product/variant) göre değişir. Form **dinamik** olmalı.

## 3. Ekran envanteri

> Her ekran için: amaç • gösterilen veri • aksiyonlar • API uçları.

### 3.1 Login
- **Amaç**: kimlik doğrulama.
- **Alanlar**: email, şifre, (opsiyonel) tenant slug.
- **Aksiyon**: Giriş → token sakla (memory + httpOnly tercih; v1 bearer).
- **API**: `POST /login {email, password, tenant_slug?}` → `{token, expires_at, tenant{slug,role}}`.

### 3.2 Uygulama kabuğu (layout)
- Sol **sidebar** navigasyon: Panel · Tanımlar (Kategoriler, Özellikler, Metaobject'ler) ·
  Ürünler · Medya · (owner ise) Admin.
- Üst bar: aktif tenant adı, kullanıcı menüsü (çıkış), rol rozeti.
- İçerik alanı: tablo/form sayfaları.

### 3.3 Panel (Dashboard)
- **Veri**: grup/ürün/varyant sayıları, son eklenen gruplar, taslak vs aktif dağılımı.
- **API**: `GET /groups` (sayım için), `GET /me`.

### 3.4 Tanımlar → Kategoriler
- **Amaç**: kategori **ağacı** yönetimi (parent_id ile).
- **Veri**: ağaç görünümü (ad, code). Seçilince sağ panelde detay.
- **Aksiyon**: ekle/düzenle/sil; alt kategori ekle.
- **API**: `GET/POST /categories`, `GET/PATCH/DELETE /categories/{id}`.

### 3.5 Kategori detayı → Atanmış özellikler & pazaryeri eşleme
- **Veri**: kategoriye atanmış attribute'lar (key, label, data_type, binding_level,
  **required**, marketplace_required, sıra). Pazaryeri kategori/attribute eşlemeleri.
- **Aksiyon**: attribute ata (required işaretle), sırala, kaldır; pazaryeri map ekle.
- **API**: `GET/POST /categories/{id}/attributes`, `PATCH/DELETE /category-attributes/{id}`,
  `.../marketplace-category-map`, `.../marketplace-attribute-map`.

### 3.6 Tanımlar → Özellikler (Attributes)
- **Amaç**: attribute tanımları.
- **Alanlar**: key, label, **data_type** (text|number|bool|date|money|dimension|color|
  single_select|multi_select|metaobject_ref|metaobject_list), **value_source**
  (none|inline|metaobject), inline_options (select için), metaobject_definition_id,
  **binding_level** (group|product|variant), is_global.
- **Tasarım notu**: data_type ↔ value_source bağımlı — form koşullu (ör. metaobject_ref
  seçilince metaobject tanımı seçtir; single_select inline seçilince seçenek listesi).
- **API**: `GET/POST /attributes`, `GET/PATCH/DELETE /attributes/{id}`.

### 3.7 Tanımlar → Metaobject'ler (ör. Renk, Beden)
- **Amaç**: yapılandırılmış değer kümeleri. Seed: **Renk**{ad,hex}, **Beden**{ad}.
- **Veri**: tanım listesi → seçilince alanları (fields) + kayıtları (entries).
- **Aksiyon**: tanım ekle; alan ekle (key,label,data_type); kayıt ekle
  (ör. Kırmızı{ad,hex}, Beyaz{ad}). Kayıt değerleri alanlara göre **dinamik form**.
- **API**: `GET/POST /metaobject-definitions`, `.../{id}/fields`, `.../{id}/entries`,
  `GET/PATCH/DELETE /metaobject-entries/{id}`.

### 3.8 Ürünler → Grup listesi
- **Veri**: grup tablosu (group_code, başlık, kategori, **durum** taslak/aktif,
  ürün/varyant sayısı, güncellenme). Filtre: durum, kategori, arama.
- **Aksiyon**: "Ürün Oluştur" (batch), gruba git, sil.
- **API**: `GET /groups`.

### 3.9 ⭐ Ürün Oluştur (Batch builder) — en kritik ekran
- **Amaç**: tek formda grup → ürünler → varyantlar ağacını kurmak (`products:batch`).
- **Yapı (3 seviye)**:
  1. **Grup**: group_code (boşsa otomatik), kategori seç, başlık, durum (taslak/aktif),
     grup-seviyesi attribute alanları (kategoriye göre dinamik).
  2. **Ürünler** (renk bazlı, tekrarlı): her ürün için kod (ör. R01) / sku override,
     grouping entry (renk), başlık, ürün-seviyesi attribute'lar.
  3. **Varyantlar** (her ürün altında, **ragged** — ürünler farklı beden setlerine
     sahip olabilir): axis_value (beden), price, compare_at_price, stock,
     barcode override (boşsa otomatik EAN-13), varyant-seviyesi attribute'lar.
- **Önemli UX**: ürün başına varyant satırlarını hızlı eklemek (beden çoklu seç →
  satır üret), kod/barkod otomatik üretileceğinin belirtilmesi, taslakta zorunlu
  alan esnek / aktife geçerken zorunlu (hata 400 → alanları işaretle).
- **API**: `POST /products:batch` (tek çağrı). Yanıt: oluşan ağaç (id'ler, üretilen
  sku/barcode'lar).

### 3.10 Grup detayı (ağaç görünümü)
- **Veri**: grup + ürünleri + her ürünün varyantları (sku, barcode, fiyat, stok),
  ürün medyaları.
- **Aksiyon**: alan düzenle (PATCH), durum değiştir (taslak↔aktif), ürün/varyant sil.
- **API**: `GET /groups/{id}` (nested), `PATCH /groups/{id}`, `GET/PATCH/DELETE
  /products/{id}`, `GET/PATCH/DELETE /variants/{id}`.

### 3.11 Medya
- **Amaç**: ürün görselleri (medya **ürün** seviyesinde; varyantlar miras alır).
- **Aksiyon**: tekil yükle (ürüne); **toplu yükle** (dosya adı = product_sku → otomatik
  eşleşir, eşleşmeyenler "atlandı" raporu); nadir varyant override.
- **API**: `POST /products/{id}/media` (multipart `file`), `POST /media:bulk`
  (multipart `files`, dosya adı=sku), `GET /products/{id}/media`, `DELETE /media/{id}`.

### 3.12 Admin (platform — owner/operatör)
- **Amaç**: başvuru onayı (→ tenant provision), tenant listesi, modül flag aç/kapa.
- **Auth**: ayrı **X-Admin-Token** başlığı (kullanıcı JWT'si değil). Tasarımda ayrı
  "Admin" bölümü; token girişi/saklama.
- **Aksiyon**: başvuru listele/onayla (onayda owner şifresi tek sefer gösterilir),
  tenant listele, modül (pim/integration/wms) aç/kapa.
- **API**: `GET/POST /admin/applications`, `POST /admin/applications/{id}/approve`,
  `GET /admin/tenants`, `POST /admin/tenants/{id}/modules/{module} {enabled}`.

### 3.13 Modül-gated örnek
- `GET /integration/status` → integration modülü kapalıysa 403 (Entegrasyon menüsü
  flag'e göre gizlenir/pasifleşir).

## 4. Tasarım dili önerisi (token'lar)
- **Karakter**: temiz, yoğun-veri dostu B2B admin (Linear/Stripe/Shopify-admin hissi).
- **Renk**: nötr gri zemin + tek vurgu rengi; durum renkleri (taslak=gri, aktif=yeşil,
  arşiv=sarı; hata=kırmızı). Renk metaobject'i için küçük hex swatch'lar.
- **Tipografi**: okunaklı sans (Inter vb.), tablo için tabular-nums.
- **Bileşenler**: veri tablosu (sıralama/filtre/sayfalama), ağaç görünümü, çok-seviyeli
  builder, dinamik form alanları (data_type'a göre input türü), dosya yükleme
  (drag-drop + toplu), toast, boş-durum, doğrulama hatası gösterimi.
- **Erişilebilirlik**: form etiketleri, klavye, hata özetleri.

## 5. Teknik entegrasyon (frontend kurulurken)
- Stack önerisi: **Vite + React + TypeScript**, `web/` altında.
- API base URL env'den (`VITE_API_BASE_URL`, ör. `http://localhost:8080`).
- Bearer token interceptor; 401 → logout. Hata envelope'unu ortak handler'da çöz.
- Multipart yüklemeler için `FormData` (single: `file`, bulk: `files`).
- Tipler: API yanıtları backend modelleriyle birebir (id'ler uuid, jsonb alanlar obje).

## 6. Akış (design-first döngü)
1. Bu brief'i claude.ai Design'a ver → ekran mockup'ları + bileşen/token üret.
2. Beğenince component'leri/tasarım sistemini al (gerekirse `/design-sync` ile org'a yükle).
3. Buraya dön → ben `web/`'i kurar, bileşenleri yerleştirir, API'ye bağlarım.
4. Lokalde `docker compose up` + `pimly serve` + `web` dev server → ekranları tıklayarak test.
