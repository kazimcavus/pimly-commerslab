// Package migrations, şema başına SQL migration dosyalarını binary'ye gömer.
// Her alt klasör bir Postgres şemasına karşılık gelir (catalog, channels,
// identity, inventory, pricing). 0001_baseline, mevcut .NET/EF şemasının
// pg_dump çıktısıdır; 0002 ve sonrası Go dönemi değişiklikleridir.
// Uygulama kuralları için bkz. internal/platform/pg.
package migrations

import "embed"

// FS, tüm migration dosyalarını içeren gömülü dosya sistemidir.
//
//go:embed */*.sql
var FS embed.FS
