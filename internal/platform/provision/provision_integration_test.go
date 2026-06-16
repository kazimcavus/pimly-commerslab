//go:build integration

package provision_test

import (
	"context"
	"errors"
	"testing"

	"github.com/jackc/pgx/v5"

	"github.com/kazimcavus/pimly/internal/platform/db/dbtest"
	"github.com/kazimcavus/pimly/internal/platform/db/tenantdb"
	"github.com/kazimcavus/pimly/internal/platform/provision"
)

func TestCreateTenant_ProvisionsAndSeeds(t *testing.T) {
	ctx := context.Background()
	database := dbtest.New(t)

	res, err := provision.CreateTenant(ctx, database, provision.Input{
		Name: "Acme Tekstil", OwnerEmail: "owner@acme.test", OwnerName: "Owner",
	})
	if err != nil {
		t.Fatalf("CreateTenant: %v", err)
	}
	if res.Tenant.SchemaName != "tenant_acme_tekstil" {
		t.Fatalf("schema = %q, want tenant_acme_tekstil", res.Tenant.SchemaName)
	}
	if res.AppliedMigrations != 1 {
		t.Fatalf("applied migrations = %d, want 1", res.AppliedMigrations)
	}
	if res.GeneratedPassword == "" {
		t.Fatal("expected a generated password for a new owner")
	}

	if err := database.WithTenant(ctx, res.Tenant.SchemaName, func(tx pgx.Tx) error {
		q := tenantdb.New(tx)
		defs, err := q.ListMetaobjectDefinitions(ctx)
		if err != nil {
			return err
		}
		if len(defs) != 2 {
			t.Fatalf("seed defs = %d, want 2", len(defs))
		}
		attrs, err := q.ListAttributes(ctx)
		if err != nil {
			return err
		}
		if len(attrs) != 2 {
			t.Fatalf("seed attrs = %d, want 2", len(attrs))
		}
		return nil
	}); err != nil {
		t.Fatalf("inspect tenant: %v", err)
	}
}

// TestCreateTenant_Isolation proves tenant A's data is invisible to tenant B,
// reusing the SAME pool across A→B→A to confirm no SQLSTATE 0A000 plan-cache
// error and no cross-tenant leakage.
func TestCreateTenant_Isolation(t *testing.T) {
	ctx := context.Background()
	database := dbtest.New(t)

	a, err := provision.CreateTenant(ctx, database, provision.Input{Name: "Tenant A", OwnerEmail: "a@x.test"})
	if err != nil {
		t.Fatalf("create A: %v", err)
	}
	b, err := provision.CreateTenant(ctx, database, provision.Input{Name: "Tenant B", OwnerEmail: "b@x.test"})
	if err != nil {
		t.Fatalf("create B: %v", err)
	}

	if err := database.WithTenant(ctx, a.Tenant.SchemaName, func(tx pgx.Tx) error {
		_, err := tenantdb.New(tx).CreateMetaobjectDefinition(ctx, tenantdb.CreateMetaobjectDefinitionParams{Key: "marka", Label: "Marka"})
		return err
	}); err != nil {
		t.Fatalf("insert into A: %v", err)
	}

	if err := database.WithTenant(ctx, b.Tenant.SchemaName, func(tx pgx.Tx) error {
		_, err := tenantdb.New(tx).GetMetaobjectDefinitionByKey(ctx, "marka")
		if err == nil {
			t.Fatal("tenant B unexpectedly sees tenant A's 'marka' definition")
		}
		if !errors.Is(err, pgx.ErrNoRows) {
			return err
		}
		return nil
	}); err != nil {
		t.Fatalf("inspect B: %v", err)
	}

	if err := database.WithTenant(ctx, a.Tenant.SchemaName, func(tx pgx.Tx) error {
		_, err := tenantdb.New(tx).GetMetaobjectDefinitionByKey(ctx, "marka")
		return err
	}); err != nil {
		t.Fatalf("tenant A should still see its own 'marka': %v", err)
	}
}

// TestCreateTenant_RollbackOnFailure forces CREATE SCHEMA to fail (the target
// schema is pre-created) and asserts the public bookkeeping rows roll back.
func TestCreateTenant_RollbackOnFailure(t *testing.T) {
	ctx := context.Background()
	database := dbtest.New(t)

	if err := database.Tx(ctx, func(tx pgx.Tx) error {
		_, err := tx.Exec(ctx, "CREATE SCHEMA tenant_rollback_test")
		return err
	}); err != nil {
		t.Fatalf("pre-create schema: %v", err)
	}

	if _, err := provision.CreateTenant(ctx, database, provision.Input{Name: "Rollback Test", OwnerEmail: "rb@x.test"}); err == nil {
		t.Fatal("expected provisioning to fail when schema already exists")
	}

	var tenants, users int
	if err := database.Tx(ctx, func(tx pgx.Tx) error {
		if err := tx.QueryRow(ctx, "SELECT count(*) FROM tenants WHERE slug = 'rollback_test'").Scan(&tenants); err != nil {
			return err
		}
		return tx.QueryRow(ctx, "SELECT count(*) FROM users WHERE email = 'rb@x.test'").Scan(&users)
	}); err != nil {
		t.Fatalf("count after rollback: %v", err)
	}
	if tenants != 0 {
		t.Fatalf("expected 0 tenant rows after rollback, got %d", tenants)
	}
	if users != 0 {
		t.Fatalf("expected owner user rolled back, got %d", users)
	}
}
