-- Faz 8a: stok lokasyonu.
--
-- Bugün stock_levels kalem başına TEK satır tutuyor (benzersiz indeks
-- product_item_id üzerinde). Shopify stoğu lokasyon başına tuttuğu için bu
-- varsayım ikinci kanalda kırılıyor: lokasyon seçilmeden yazılan stok ya hata
-- verir ya yanlış depoya gider.
--
-- Şimdi yapmak bedava (canlı veri yok), sonra yapmak her tenant'ta benzersiz
-- indeks değişimi demek — yani kilitlenme riski olan bir göç.

CREATE TABLE IF NOT EXISTS inventory.locations (
    id          uuid         PRIMARY KEY,
    tenant_id   uuid         NOT NULL,
    code        varchar(50)  NOT NULL,
    name        varchar(200) NOT NULL,
    is_default  boolean      NOT NULL DEFAULT false,
    created_at  timestamptz  NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_locations_tenant_code
    ON inventory.locations (tenant_id, code);

-- Tenant başına en fazla bir varsayılan lokasyon.
CREATE UNIQUE INDEX IF NOT EXISTS ix_locations_tenant_default
    ON inventory.locations (tenant_id) WHERE is_default;

-- Stoğu olan her tenant için varsayılan depo üretilir (mevcut satırların
-- taşınacağı yer). Stoğu olmayan tenant'lara uygulama ilk stok yazımında
-- oluşturur.
INSERT INTO inventory.locations (id, tenant_id, code, name, is_default)
SELECT gen_random_uuid(), s.tenant_id, 'MAIN', 'Ana Depo', true
  FROM (SELECT DISTINCT tenant_id FROM inventory.stock_levels) s
 WHERE NOT EXISTS (
       SELECT 1 FROM inventory.locations l
        WHERE l.tenant_id = s.tenant_id AND l.code = 'MAIN');

ALTER TABLE inventory.stock_levels
    ADD COLUMN IF NOT EXISTS location_id uuid;

-- Mevcut stok satırları tenant'ın varsayılan deposuna taşınır.
UPDATE inventory.stock_levels s
   SET location_id = l.id
  FROM inventory.locations l
 WHERE l.tenant_id = s.tenant_id
   AND l.is_default
   AND s.location_id IS NULL;

ALTER TABLE inventory.stock_levels
    ALTER COLUMN location_id SET NOT NULL;

ALTER TABLE inventory.stock_levels
    ADD CONSTRAINT fk_stock_levels_location
    FOREIGN KEY (location_id) REFERENCES inventory.locations (id);

-- Benzersizlik artık (kalem, lokasyon) ikilisinde: aynı kalem birden fazla
-- depoda stok taşıyabilir. Eski tek-kolonlu indeks kaldırılıyor.
DROP INDEX IF EXISTS inventory."IX_stock_levels_product_item_id";

CREATE UNIQUE INDEX IF NOT EXISTS ix_stock_levels_item_location
    ON inventory.stock_levels (product_item_id, location_id);

-- Kalem bazlı toplam stok sorgusu (kanala gönderilecek miktar) için.
CREATE INDEX IF NOT EXISTS ix_stock_levels_item
    ON inventory.stock_levels (product_item_id);
