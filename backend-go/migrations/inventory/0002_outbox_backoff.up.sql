-- Go dönemi eklentisi: outbox dispatcher'ına üstel backoff ve dead-letter
-- desteği. Kolon nullable olduğundan .NET/EF tarafı görmeden çalışmaya devam
-- eder (EF kolonları açıkça eşler); yan yana çalışma güvenlidir.
ALTER TABLE inventory.outbox_messages ADD COLUMN IF NOT EXISTS next_attempt_at timestamptz;

-- Bekleyen mesaj taraması için kısmi indeks (işlenmişler indekse girmez).
CREATE INDEX IF NOT EXISTS ix_inventory_outbox_pending
    ON inventory.outbox_messages (occurred_on_utc)
    WHERE processed_on_utc IS NULL;
