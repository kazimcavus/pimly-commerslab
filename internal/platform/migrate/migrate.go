// Package migrate runs the global (public schema) migrations via golang-migrate.
// It isolates the heavy golang-migrate dependency to a single small package that
// the CLI and integration tests share.
package migrate

import (
	"database/sql"
	"errors"
	"fmt"

	"github.com/golang-migrate/migrate/v4"
	migratepgx "github.com/golang-migrate/migrate/v4/database/pgx/v5"
	"github.com/golang-migrate/migrate/v4/source/iofs"
	_ "github.com/jackc/pgx/v5/stdlib" // registers the "pgx" database/sql driver

	"github.com/kazimcavus/pimly/migrations"
)

// RunGlobal applies all pending global migrations to the database at databaseURL.
func RunGlobal(databaseURL string) error {
	src, err := iofs.New(migrations.GlobalFS, "global")
	if err != nil {
		return fmt.Errorf("open migration source: %w", err)
	}
	sqldb, err := sql.Open("pgx", databaseURL)
	if err != nil {
		return fmt.Errorf("open db: %w", err)
	}
	defer sqldb.Close()

	drv, err := migratepgx.WithInstance(sqldb, &migratepgx.Config{})
	if err != nil {
		return fmt.Errorf("migrate driver: %w", err)
	}
	m, err := migrate.NewWithInstance("iofs", src, "pgx5", drv)
	if err != nil {
		return fmt.Errorf("migrate init: %w", err)
	}
	if err := m.Up(); err != nil && !errors.Is(err, migrate.ErrNoChange) {
		return fmt.Errorf("migrate up: %w", err)
	}
	return nil
}
