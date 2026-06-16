//go:build integration

// Package dbtest spins up throwaway databases for integration tests. Each call
// to New creates a fresh database (migrated to the latest global schema) and
// drops it on cleanup. Tests are skipped when no test database is reachable, so
// `go test` without -tags=integration (or without Docker) stays green.
package dbtest

import (
	"context"
	"crypto/rand"
	"encoding/hex"
	"net/url"
	"os"
	"testing"

	"github.com/jackc/pgx/v5"

	"github.com/kazimcavus/pimly/internal/platform/db"
	"github.com/kazimcavus/pimly/internal/platform/migrate"
)

const defaultURL = "postgres://pimly:pimly@localhost:5432/pimly?sslmode=disable"

func baseURL() string {
	if u := os.Getenv("PIMLY_TEST_DATABASE_URL"); u != "" {
		return u
	}
	return defaultURL
}

// New returns a *db.DB backed by a fresh, migrated, throwaway database.
func New(t *testing.T) *db.DB {
	t.Helper()
	ctx := context.Background()

	admin, err := pgx.Connect(ctx, baseURL())
	if err != nil {
		t.Skipf("skipping integration test: test database unreachable: %v", err)
	}
	buf := make([]byte, 6)
	_, _ = rand.Read(buf)
	name := "pimly_test_" + hex.EncodeToString(buf)
	if _, err := admin.Exec(ctx, "CREATE DATABASE "+pgx.Identifier{name}.Sanitize()); err != nil {
		admin.Close(ctx)
		t.Fatalf("create test database: %v", err)
	}
	admin.Close(ctx)

	testURL := withDBName(t, baseURL(), name)
	if err := migrate.RunGlobal(testURL); err != nil {
		dropDB(t, name)
		t.Fatalf("run global migrations: %v", err)
	}
	database, err := db.New(ctx, testURL, 4, 0)
	if err != nil {
		dropDB(t, name)
		t.Fatalf("open test db pool: %v", err)
	}
	t.Cleanup(func() {
		database.Close()
		dropDB(t, name)
	})
	return database
}

func withDBName(t *testing.T, raw, name string) string {
	u, err := url.Parse(raw)
	if err != nil {
		t.Fatalf("parse db url: %v", err)
	}
	u.Path = "/" + name
	return u.String()
}

func dropDB(t *testing.T, name string) {
	ctx := context.Background()
	admin, err := pgx.Connect(ctx, baseURL())
	if err != nil {
		t.Logf("cleanup: cannot connect to drop %s: %v", name, err)
		return
	}
	defer admin.Close(ctx)
	_, _ = admin.Exec(ctx, "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = $1", name)
	if _, err := admin.Exec(ctx, "DROP DATABASE IF EXISTS "+pgx.Identifier{name}.Sanitize()+" WITH (FORCE)"); err != nil {
		t.Logf("cleanup: drop database %s: %v", name, err)
	}
}
