-- Faz 8a: KDV oranı. İki ayrı ihtiyaç aynı alana bakıyor:
--   1) Shopify mağaza ayarı "fiyatlar KDV hariç" ise, oran bilinmeden
--      gönderilen fiyat KDV kadar sapar (bkz. channels.prices_include_vat).
--   2) E-fatura fazının ön koşulu; faturada satır başına oran zorunlu.
-- Bugün bir kolon, altı ay sonra her tenant'ta veri göçü.
--
-- KDV ürün özelliğidir (halı %20'dir, hangi kanala gittiğinden bağımsız),
-- o yüzden kalemde değil üründe tutuluyor. Mevcut satırlar için bilinmiyor:
-- nullable bırakılıp tenant varsayılanına düşülüyor.
ALTER TABLE catalog.products
    ADD COLUMN IF NOT EXISTS vat_rate numeric(5,2);

ALTER TABLE catalog.products
    ADD CONSTRAINT ck_products_vat_rate_range
    CHECK (vat_rate IS NULL OR (vat_rate >= 0 AND vat_rate <= 100));

-- Tenant varsayılanı: ürününde oran girilmemişse bu kullanılır. Türkiye'de
-- genel oran %20; kullanıcı Ayarlar'dan değiştirebilir.
ALTER TABLE catalog.catalog_settings
    ADD COLUMN IF NOT EXISTS default_vat_rate numeric(5,2) NOT NULL DEFAULT 20.00;

ALTER TABLE catalog.catalog_settings
    ADD CONSTRAINT ck_catalog_settings_default_vat_rate_range
    CHECK (default_vat_rate >= 0 AND default_vat_rate <= 100);
