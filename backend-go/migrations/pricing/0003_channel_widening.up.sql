-- Faz 8a: channels.marketplace_code ile aynı genişletme (bkz.
-- channels/0002_channel_widening). Bu kolon channels tarafındakiyle birlikte
-- hareket etmek zorunda: dar kalırsa "hepsiburada" fiyatı yazılamaz.
ALTER TABLE pricing.channel_prices ALTER COLUMN marketplace_code TYPE varchar(32);

-- Bağlantı başına fiyat kaynağı (kullanıcı kararı): kanal fiyatı elle
-- girilmek zorunda değil; "baz fiyatı kullan" ya da adlandırılmış bir fiyat
-- listesine bağlanmak da açık birer tercihtir. Korunan invaryant fiyatın elle
-- yazılmış olması değil, AÇIK BİR KARAR verilmiş olmasıdır.
--
--   base       → pricing.base_prices aynen kullanılır
--   definition → price_definition_id'deki liste kullanılır
--   manual     → yalnızca açıkça girilmiş channel_prices satırları (eski davranış)
--
-- Hangi kaynak seçilirse seçilsin çözülen fiyat channel_prices satırı olarak
-- ÜRETİLİR; böylece her gönderim denetlenebilir kalır ve "bu fiyat neden
-- böyle?" sorusu cevaplanabilir.
CREATE TABLE IF NOT EXISTS pricing.channel_price_sources (
    id                    uuid         PRIMARY KEY,
    tenant_id             uuid         NOT NULL,
    marketplace_code      varchar(32)  NOT NULL,
    source_kind           varchar(20)  NOT NULL DEFAULT 'manual',
    price_definition_id   uuid,
    updated_at            timestamptz  NOT NULL DEFAULT now(),
    CONSTRAINT ck_channel_price_sources_kind
        CHECK (source_kind IN ('manual', 'base', 'definition')),
    -- 'definition' seçildiyse hangi listenin kullanılacağı belirtilmek zorunda.
    CONSTRAINT ck_channel_price_sources_definition_required
        CHECK (source_kind <> 'definition' OR price_definition_id IS NOT NULL)
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_channel_price_sources_tenant_marketplace
    ON pricing.channel_price_sources (tenant_id, marketplace_code);
