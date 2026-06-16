// Package db owns the pgx connection pool and the tenant-scoping execution
// helpers. It deliberately knows nothing about generated query packages so it
// stays a low-level dependency.
package db

import (
	"context"
	"fmt"

	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgxpool"

	"github.com/kazimcavus/pimly/internal/platform/tenant"
)

type DB struct {
	pool *pgxpool.Pool
}

// New builds a pool from dsn. It forces QueryExecModeExec so that no server-side
// prepared statements are cached: with schema-per-tenant, a cached plan reused
// on a connection whose search_path later points at a different (structurally
// identical) schema triggers "cached plan must not change result type"
// (SQLSTATE 0A000). Exec mode keeps the extended protocol (typed/binary params)
// while skipping statement caching.
func New(ctx context.Context, dsn string, maxConns, minConns int32) (*DB, error) {
	cfg, err := pgxpool.ParseConfig(dsn)
	if err != nil {
		return nil, fmt.Errorf("parse db config: %w", err)
	}
	cfg.ConnConfig.DefaultQueryExecMode = pgx.QueryExecModeExec
	if maxConns > 0 {
		cfg.MaxConns = maxConns
	}
	if minConns >= 0 {
		cfg.MinConns = minConns
	}
	pool, err := pgxpool.NewWithConfig(ctx, cfg)
	if err != nil {
		return nil, fmt.Errorf("connect db: %w", err)
	}
	if err := pool.Ping(ctx); err != nil {
		pool.Close()
		return nil, fmt.Errorf("ping db: %w", err)
	}
	return &DB{pool: pool}, nil
}

// Pool exposes the underlying pool (used by the global migration runner).
func (d *DB) Pool() *pgxpool.Pool { return d.pool }

// Close releases all connections.
func (d *DB) Close() { d.pool.Close() }

// Ping verifies connectivity.
func (d *DB) Ping(ctx context.Context) error { return d.pool.Ping(ctx) }

// Tx runs fn inside a public-schema transaction, committing on success and
// rolling back on error.
func (d *DB) Tx(ctx context.Context, fn func(pgx.Tx) error) error {
	return pgx.BeginFunc(ctx, d.pool, fn)
}

// WithTenant runs fn inside a transaction whose search_path is set (via
// SET LOCAL) to "<schema>, public". Because SET LOCAL is transaction-scoped,
// the connection reverts to its default search_path when returned to the pool —
// preventing any cross-tenant leakage.
func (d *DB) WithTenant(ctx context.Context, schema string, fn func(pgx.Tx) error) error {
	if err := tenant.ValidateSchemaName(schema); err != nil {
		return err
	}
	return pgx.BeginFunc(ctx, d.pool, func(tx pgx.Tx) error {
		// search_path cannot be a bound parameter; schema is validated above and
		// quoted as an identifier.
		stmt := "SET LOCAL search_path = " + pgx.Identifier{schema}.Sanitize() + ", public"
		if _, err := tx.Exec(ctx, stmt); err != nil {
			return fmt.Errorf("set search_path: %w", err)
		}
		return fn(tx)
	})
}
