// Package pg, Postgres bağlantı havuzunu ve şema migration'larını yönetir.
// .NET tarafındaki EF Core DbContext kaydı + ApplyXMigrationsAsync
// uzantılarının karşılığıdır:
//
//   - Bağlantı dizeleri .NET/Npgsql biçiminde gelir (Host=..;Port=..;...) ve
//     pgx URL'sine çevrilir; böylece mevcut env dosyaları değişmeden çalışır.
//   - Her modül şeması kendi migration geçmişini kendi şemasındaki
//     schema_migrations tablosunda tutar (EF'in __ef_migrations_history'sine
//     paralel; EF tabloları denetim izi olarak yerinde bırakılır).
//   - Baseline damgalama: şema EF tarafından zaten kurulmuşsa (mevcut veritabanı),
//     0001_baseline dosyası ÇALIŞTIRILMADAN sürüm 1 olarak işaretlenir; taze
//     veritabanında ise gerçekten çalıştırılır. Böylece aynı binary hem mevcut
//     VPS veritabanına hem boş bir geliştirme/test veritabanına karşı açılabilir.
package pg

import (
	"context"
	"database/sql"
	"errors"
	"fmt"
	"log/slog"
	"net/url"
	"strings"

	"github.com/golang-migrate/migrate/v4"
	migratepg "github.com/golang-migrate/migrate/v4/database/postgres"
	"github.com/golang-migrate/migrate/v4/source/iofs"
	"github.com/jackc/pgx/v5/pgxpool"
	_ "github.com/jackc/pgx/v5/stdlib" // migrate için database/sql sürücüsü

	"pimly.commerslab/backend-go/migrations"
)

// ConnURL, .NET/Npgsql biçimindeki bağlantı dizesini (Host=..;Port=..;
// Database=..;Username=..;Password=..) pgx'in anladığı postgres:// URL'sine çevirir.
func ConnURL(dotnetConnString string) (string, error) {
	values := map[string]string{}
	for _, part := range strings.Split(dotnetConnString, ";") {
		part = strings.TrimSpace(part)
		if part == "" {
			continue
		}
		key, value, ok := strings.Cut(part, "=")
		if !ok {
			return "", fmt.Errorf("pg: bağlantı dizesi çözümlenemedi: %q", part)
		}
		values[strings.ToLower(strings.TrimSpace(key))] = strings.TrimSpace(value)
	}

	host := values["host"]
	if host == "" {
		return "", errors.New("pg: bağlantı dizesinde Host yok")
	}
	port := values["port"]
	if port == "" {
		port = "5432"
	}
	u := url.URL{
		Scheme: "postgres",
		User:   url.UserPassword(values["username"], values["password"]),
		Host:   host + ":" + port,
		Path:   "/" + values["database"],
	}
	query := url.Values{}
	if values["ssl mode"] == "" && values["sslmode"] == "" {
		query.Set("sslmode", "disable")
	}
	u.RawQuery = query.Encode()
	return u.String(), nil
}

// NewPool, verilen .NET biçimli bağlantı dizesiyle bir pgx bağlantı havuzu
// açar ve ilk bağlantıyı ping ile doğrular.
func NewPool(ctx context.Context, dotnetConnString string) (*pgxpool.Pool, error) {
	connURL, err := ConnURL(dotnetConnString)
	if err != nil {
		return nil, err
	}
	pool, err := pgxpool.New(ctx, connURL)
	if err != nil {
		return nil, fmt.Errorf("pg: havuz oluşturulamadı: %w", err)
	}
	if err := pool.Ping(ctx); err != nil {
		pool.Close()
		return nil, fmt.Errorf("pg: veritabanına ulaşılamadı: %w", err)
	}
	return pool, nil
}

// Migrate, verilen şemanın migration'larını uygular. Şema EF tarafından zaten
// kurulmuşsa ve Go migration geçmişi boşsa, baseline çalıştırılmadan sürüm 1
// damgalanır; ardından (her iki durumda) bekleyen migration'lar çalıştırılır.
func Migrate(ctx context.Context, dotnetConnString, schema string) error {
	connURL, err := ConnURL(dotnetConnString)
	if err != nil {
		return err
	}
	db, err := sql.Open("pgx", connURL)
	if err != nil {
		return fmt.Errorf("pg: migration bağlantısı açılamadı: %w", err)
	}
	defer db.Close()

	// Şema, migrate sürücüsünün schema_migrations tablosunu oluşturabilmesi
	// için önceden var olmalıdır (baseline'ın kendisi de IF NOT EXISTS kullanır).
	if _, err := db.ExecContext(ctx, "CREATE SCHEMA IF NOT EXISTS "+schema); err != nil {
		return fmt.Errorf("pg: %s şeması oluşturulamadı: %w", schema, err)
	}

	source, err := iofs.New(migrations.FS, schema)
	if err != nil {
		return fmt.Errorf("pg: %s migration kaynağı açılamadı: %w", schema, err)
	}
	driver, err := migratepg.WithInstance(db, &migratepg.Config{
		SchemaName:      schema,
		MigrationsTable: "schema_migrations",
	})
	if err != nil {
		return fmt.Errorf("pg: %s migration sürücüsü kurulamadı: %w", schema, err)
	}
	m, err := migrate.NewWithInstance("iofs", source, "pgx", driver)
	if err != nil {
		return fmt.Errorf("pg: %s migrator kurulamadı: %w", schema, err)
	}

	_, _, verr := m.Version()
	if errors.Is(verr, migrate.ErrNilVersion) {
		seeded, err := schemaHasEFHistory(ctx, db, schema)
		if err != nil {
			return err
		}
		if seeded {
			slog.Info("Existing EF schema detected; stamping baseline without executing.",
				slog.String("Schema", schema))
			if err := m.Force(1); err != nil {
				return fmt.Errorf("pg: %s baseline damgalanamadı: %w", schema, err)
			}
		}
	} else if verr != nil {
		return fmt.Errorf("pg: %s migration sürümü okunamadı: %w", schema, verr)
	}

	if err := m.Up(); err != nil && !errors.Is(err, migrate.ErrNoChange) {
		return fmt.Errorf("pg: %s migration uygulanamadı: %w", schema, err)
	}
	slog.Info("Schema migrations applied.", slog.String("Schema", schema))
	return nil
}

// schemaHasEFHistory, şemada EF migration geçmişi tablosunun var olup olmadığını
// döner; varlığı şemanın .NET tarafından kurulduğunun kanıtıdır.
func schemaHasEFHistory(ctx context.Context, db *sql.DB, schema string) (bool, error) {
	const query = `SELECT EXISTS (
		SELECT 1 FROM information_schema.tables
		WHERE table_schema = $1 AND table_name = '__ef_migrations_history')`
	var exists bool
	if err := db.QueryRowContext(ctx, query, schema).Scan(&exists); err != nil {
		return false, fmt.Errorf("pg: %s şema geçmişi sorgulanamadı: %w", schema, err)
	}
	return exists, nil
}
