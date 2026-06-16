package tenant

import (
	"context"
	"crypto/sha256"
	"encoding/hex"
	"fmt"
	"io/fs"
	"sort"
	"strconv"
	"strings"

	"github.com/jackc/pgx/v5"

	"github.com/kazimcavus/pimly/migrations"
)

// schemaVersionDDL creates the per-tenant migration bookkeeping table. It is
// ensured (idempotently) before every run rather than being a versioned
// migration itself.
const schemaVersionDDL = `
CREATE TABLE IF NOT EXISTS schema_version (
    version    integer     NOT NULL PRIMARY KEY,
    name       text        NOT NULL,
    checksum   text        NOT NULL,
    applied_at timestamptz NOT NULL DEFAULT now()
)`

// TemplateMigration is one parsed tenant-template SQL file.
type TemplateMigration struct {
	Version  int
	Name     string
	SQL      string
	Checksum string
}

// LoadTemplateMigrations parses and version-sorts the embedded tenant template
// files, each named "<version>_<name>.sql" (e.g. 001_init_core.sql).
func LoadTemplateMigrations() ([]TemplateMigration, error) {
	entries, err := fs.ReadDir(migrations.TenantTemplateFS, "tenant_template")
	if err != nil {
		return nil, fmt.Errorf("read template dir: %w", err)
	}
	var migs []TemplateMigration
	for _, e := range entries {
		if e.IsDir() || !strings.HasSuffix(e.Name(), ".sql") {
			continue
		}
		verStr, _, ok := strings.Cut(strings.TrimSuffix(e.Name(), ".sql"), "_")
		if !ok {
			return nil, fmt.Errorf("bad template filename %q (want <version>_<name>.sql)", e.Name())
		}
		ver, err := strconv.Atoi(verStr)
		if err != nil {
			return nil, fmt.Errorf("bad version in %q: %w", e.Name(), err)
		}
		data, err := fs.ReadFile(migrations.TenantTemplateFS, "tenant_template/"+e.Name())
		if err != nil {
			return nil, fmt.Errorf("read %q: %w", e.Name(), err)
		}
		sum := sha256.Sum256(data)
		migs = append(migs, TemplateMigration{
			Version:  ver,
			Name:     e.Name(),
			SQL:      string(data),
			Checksum: hex.EncodeToString(sum[:]),
		})
	}
	sort.Slice(migs, func(i, j int) bool { return migs[i].Version < migs[j].Version })
	return migs, nil
}

// LatestTemplateVersion returns the highest version among migs (0 if empty).
func LatestTemplateVersion(migs []TemplateMigration) int {
	v := 0
	for _, m := range migs {
		if m.Version > v {
			v = m.Version
		}
	}
	return v
}

// CurrentSchemaVersion ensures schema_version exists and returns the highest
// applied version in the schema selected by tx's search_path (0 if none).
func CurrentSchemaVersion(ctx context.Context, tx pgx.Tx) (int, error) {
	if _, err := tx.Exec(ctx, schemaVersionDDL); err != nil {
		return 0, fmt.Errorf("ensure schema_version: %w", err)
	}
	var v int
	if err := tx.QueryRow(ctx, "SELECT COALESCE(max(version), 0) FROM schema_version").Scan(&v); err != nil {
		return 0, fmt.Errorf("read schema_version: %w", err)
	}
	return v, nil
}

// ApplyPending applies every migration with Version greater than the current
// schema version, in order, within tx (whose search_path must already point at
// the target tenant schema). Each applied file is stamped into schema_version.
// Returns the number applied.
func ApplyPending(ctx context.Context, tx pgx.Tx, migs []TemplateMigration) (int, error) {
	cur, err := CurrentSchemaVersion(ctx, tx)
	if err != nil {
		return 0, err
	}
	applied := 0
	for _, m := range migs {
		if m.Version <= cur {
			continue
		}
		if _, err := tx.Exec(ctx, m.SQL); err != nil {
			return applied, fmt.Errorf("apply template %s: %w", m.Name, err)
		}
		if _, err := tx.Exec(ctx,
			"INSERT INTO schema_version (version, name, checksum) VALUES ($1, $2, $3)",
			m.Version, m.Name, m.Checksum); err != nil {
			return applied, fmt.Errorf("stamp template %s: %w", m.Name, err)
		}
		applied++
	}
	return applied, nil
}
