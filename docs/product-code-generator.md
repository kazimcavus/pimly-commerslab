# Ürün Kodu (SKU) Oluşturucu — Mantık Belgesi

> **Durum:** **.NET Catalog** modülünde uygulanmıştır — yapılandırma `GET/PUT /api/v1/catalog/sku-config`
> ile kalıcıdır; token/birleştirme ve atomik sayaç sunucuda çalışır. Frontend yalnızca
> şablonu yönetir ve `code_inputs` gönderir; `model_code` / varyant `sku` sunucuda üretilir.
>
> **Kaynak referans:**
> - `backend/src/Modules/Catalog/Catalog.Domain/SkuGenerator/` — token/birleştirme mantığı
> - `backend/src/Modules/Catalog/Catalog.Application/SkuGenerator/` — config API + üretim servisi
> - `web/src/lib/skuConfig.js` — API wrapper (localStorage tek seferlik migrate)
> - `web/src/screens/Settings.jsx` — segment editörü ve önizleme
> - `web/src/screens/ProductBuilder.jsx` — `code_inputs` gönderimi; önizleme (`variantSkuPreview`)

---

## 1. Amaç

Firmaların ürün/varyant kodlarını (SKU) elle yazmak yerine, sıralı **segment**lerden
oluşan yeniden kullanılabilir bir **şablon** ile otomatik üretmesini sağlar. Segmentler
firma-bağımsızdır; her segmente isteğe bağlı bir **Başlık (label)** verilebilir
(örn. "Elle girilir" tipli bir segmente "Sezon", "Sabit metin" tipli bir segmente
"Firma kodu" denebilir).

---

## 2. Veri Modeli

```jsonc
{
  "enabled": true,            // generator açık mı
  "segments": [               // sıralı segment listesi
    { "type": "fixed",   "label": "Firma kodu", "value": "26" },
    { "type": "year",    "label": "Yıl",        "digits": 2 },
    { "type": "counter", "label": "Ürün No",    "start": 1000, "width": 4 },
    { "type": "color",   "label": "Renk",       "source": "code" },
    { "type": "size",    "label": "Beden",      "source": "code" }
  ]
}
```

### Segment tipleri

| `type`    | Açıklama                              | Tipe özel alanlar                  | Kaynak                          |
|-----------|---------------------------------------|------------------------------------|---------------------------------|
| `fixed`   | Sabit metin                           | `value: string`                    | Şablonda sabit                  |
| `manual`  | Üründe elle girilir                   | —                                  | Ürün oluştururken kullanıcı     |
| `counter` | Otomatik sıralı sayaç                 | `start: int`, `width: int`         | Sayaç                           |
| `year`    | İçinde bulunulan yıl                  | `digits: 2 \| 4`                   | Sistem yılı                     |
| `color`   | Varyantın renk değeri (yalnız varyant)| `source: "code" \| "name"`         | Seçilen renk varyant değeri     |
| `size`    | Varyantın beden/ölçü değeri (yalnız varyant) | `source: "code" \| "name"`  | Seçilen beden varyant değeri    |

> `label` her tip için opsiyoneldir ve yalnızca arayüzde gösterim/etiketleme içindir;
> üretilen koda dahil edilmez.

---

## 3. Token Üretimi (segment → metin)

Her segment, üretilirken tek bir token üretir. Tüm tokenlar **büyük harfe** çevrilir.

| `type`    | Token kuralı                                                                 |
|-----------|------------------------------------------------------------------------------|
| `fixed`   | `value` (büyük harf)                                                          |
| `manual`  | Üründe girilen değer (büyük harf) — **zorunlu**                               |
| `counter` | `start` değeri, `width` haneye sol-sıfır dolgulu (örn. `1000` / width 4 → `1000`, `7` / width 4 → `0007`) |
| `year`    | `digits === 4` → tam yıl (`2025`); aksi halde son 2 hane (`yıl % 100` → `25`) |
| `color`   | `source === "name"` → varyant değerinin **adı** (`label`); aksi halde **kodu** (`code`, yoksa `label`) |
| `size`    | `color` ile aynı kural (kod/ad)                                               |

---

## 4. Birleştirme Kuralı

İki ayrı kod üretilir:

### 4.1. Ürün kodu (model kodu)
Varyant **dışı** segmentlerin (`color` ve `size` **hariç**) tokenları, şablon sırasıyla
ardışık birleştirilir.

```
productCode = segments
  .filter(s => s.type !== 'color' && s.type !== 'size')
  .map(token)
  .join('')
```

### 4.2. Varyant SKU
Ürün kodu + o varyantın seçili değerlerinden gelen `color`/`size` tokenları:

- `color` segmenti: kombinasyondaki **renk** varyant değerinden token üretir.
- `size` segmenti: kombinasyondaki **renk olmayan** (beden/ölçü) varyant değer(ler)inden token üretir.

```
variantSku = productCode + (şablondaki color/size segmentlerinin sırasıyla tokenları)
```

> Önizleme arayüzü (Settings) örnek değerlerle (`sampleToken`) gösterim yapar; gerçek
> değerler ürün oluştururken (`ProductBuilder`) seçili varyantlardan gelir.

---

## 5. Doğrulama (generator açıkken)

- **`manual` segmentleri**: ürün oluştururken ilgili alan boş bırakılamaz.
- **`code` kaynaklı `color`/`size` segmentleri**: seçilen her varyant değerinin bir
  `code`'u olmalıdır. Kod yoksa kullanıcı ya Varyantlar ekranından kod ekler ya da
  segmentin kaynağını `name` yapar.
- Generator **kapalıysa** ürün kodu kullanıcı tarafından elle girilir (zorunlu alan).

---

## 6. Örnek

Şablon:

| Sıra | Segment   | Ayar                  |
|------|-----------|-----------------------|
| 1    | `fixed`   | `value = "26"`        |
| 2    | `year`    | `digits = 2`          |
| 3    | `counter` | `start = 1000, width = 4` |
| 4    | `color`   | `source = "code"`     |
| 5    | `size`    | `source = "code"`     |

Üretim (yıl 2025 varsayımıyla):

- **Ürün kodu:** `2625` + `1000` → `26251000`
- **Varyant — Kırmızı (code `R08`) / M (code `M`):** `26251000` + `R08` + `M` → `26251000R08M`
- **Varyant — Mavi (code `B01`) / L (code `L`):** `26251000B01L`

> Ayraçlar (örn. `-`) şablona `fixed` segment olarak eklenebilir.

---

## 7. .NET uygulaması

- Yapılandırma: `GET/PUT /api/v1/catalog/sku-config` — şekil: `{ enabled, segments[], counter_next_value? }`.
- Token/birleştirme kuralları bu belgedeki tablolarla birebir uygulanır (`SkuCodeAssembler`).
- `counter` segmenti PostgreSQL `UPDATE … RETURNING` ile atomik artırılır (`SkuCounterAllocator`).
- `POST /products:batch` (ve `POST /products`) generator açıkken boş `model_code` kabul eder;
  `code_inputs` (manual segment değerleri) alır; kodları sunucuda üretir.
- Barkod serisi aynı desende ayrı yaşar (`barcode-sequence`, `barcodes:allocate`).
