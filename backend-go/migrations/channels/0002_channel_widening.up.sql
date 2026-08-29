-- Faz 8a: çok kanallı yapıya hazırlık.
--
-- 1) marketplace_code varchar(10) dar geliyordu: "hepsiburada", "ciceksepeti"
--    ve "woocommerce" 11 karakter, yani bugün INSERT hatası veriyorlar.
--    varchar(32)'ye genişletiliyor — Postgres'te varchar uzatmak yalnızca
--    katalog değişikliğidir, tablo yeniden yazılmaz.
-- 2) Bağlantı kaydına Shopify'ın gerektirdiği alanlar ekleniyor: hangi
--    lokasyona stok yazılacağı ve fiyatların KDV dahil olup olmadığı.
--
-- NOT: (tenant_id, marketplace_code) benzersizliği BİLEREK korunuyor. Aynı
-- pazaryerine ikinci bağlantı, listing/mapping tablolarına connection_id
-- eklenmeden açılamaz; yoksa kimlik bilgisi çözümü belirsizleşir. O adım 8b.

ALTER TABLE channels.attribute_channel_mappings   ALTER COLUMN marketplace_code TYPE varchar(32);
ALTER TABLE channels.category_channel_mappings    ALTER COLUMN marketplace_code TYPE varchar(32);
ALTER TABLE channels.external_attribute_values    ALTER COLUMN marketplace_code TYPE varchar(32);
ALTER TABLE channels.external_categories          ALTER COLUMN marketplace_code TYPE varchar(32);
ALTER TABLE channels.external_category_attributes ALTER COLUMN marketplace_code TYPE varchar(32);
ALTER TABLE channels.marketplace_connections      ALTER COLUMN marketplace_code TYPE varchar(32);
ALTER TABLE channels.product_import_runs          ALTER COLUMN marketplace_code TYPE varchar(32);
ALTER TABLE channels.product_listings             ALTER COLUMN marketplace_code TYPE varchar(32);
ALTER TABLE channels.product_publication_runs     ALTER COLUMN marketplace_code TYPE varchar(32);
ALTER TABLE channels.taxonomy_sync_runs           ALTER COLUMN marketplace_code TYPE varchar(32);

-- Bağlantıya insan tarafından okunur ad: aynı pazaryerinde birden fazla
-- bağlantı açıldığında (8b) kullanıcı hangisi olduğunu ayırt edebilsin.
ALTER TABLE channels.marketplace_connections
    ADD COLUMN IF NOT EXISTS display_name varchar(200);

-- Shopify stoğu lokasyon başına tutar; lokasyon seçilmeden yazılan stok ya
-- hata verir ya yanlış depoya gider. Trendyol'da boş kalır.
ALTER TABLE channels.marketplace_connections
    ADD COLUMN IF NOT EXISTS external_location_id varchar(200);

-- Fiyatların KDV dahil mi gönderileceği bağlantı başına ayar. Türkiye'de
-- pazaryerleri KDV dahil çalışır (varsayılan true); Shopify mağaza ayarına
-- göre hariç olabilir ve o durumda fiyatlar KDV oranı kadar sapar.
ALTER TABLE channels.marketplace_connections
    ADD COLUMN IF NOT EXISTS prices_include_vat boolean NOT NULL DEFAULT true;

-- Mutabakat kapsam dışı bırakma kuralı: hangi kanal kayıtlarına hiç
-- dokunulmayacağı. Çağ Halı'da 1.089 varyantlık "Özel Ölçü" kaydı bunun
-- gerçek örneği — barkodsuz, PIM'de karşılığı yok, silinmemeli.
-- JSON biçimi: {"sku_patterns":["%-OZEL-%"],"statuses":["UNLISTED"]}
ALTER TABLE channels.marketplace_connections
    ADD COLUMN IF NOT EXISTS exclusion_rules jsonb NOT NULL DEFAULT '{}'::jsonb;
