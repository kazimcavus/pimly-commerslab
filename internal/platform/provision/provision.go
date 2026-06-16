// Package provision creates new tenants: it inserts the public bookkeeping rows,
// creates the tenant schema, applies the template migrations, and seeds default
// definitions — all in a single transaction so any failure rolls back atomically
// (Postgres DDL is transactional, so the schema + tables vanish on error too).
package provision

import (
	"context"
	"crypto/rand"
	"encoding/base64"
	"errors"
	"fmt"
	"time"

	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgtype"

	"github.com/kazimcavus/pimly/internal/platform/auth"
	"github.com/kazimcavus/pimly/internal/platform/db"
	"github.com/kazimcavus/pimly/internal/platform/db/globaldb"
	"github.com/kazimcavus/pimly/internal/platform/db/tenantdb"
	"github.com/kazimcavus/pimly/internal/platform/flags"
	"github.com/kazimcavus/pimly/internal/platform/tenant"
	"github.com/kazimcavus/pimly/internal/shared/apperr"
)

// Input describes a tenant to create.
type Input struct {
	Name          string
	OwnerEmail    string
	OwnerName     string
	OwnerPassword string // if empty (and the owner is new), a random one is generated
}

// Result reports what was created.
type Result struct {
	Tenant            globaldb.Tenant
	Owner             globaldb.User
	OwnerCreated      bool
	GeneratedPassword string // set only when a password was generated
	AppliedMigrations int
}

// CreateTenant provisions a new tenant end to end in one transaction.
func CreateTenant(ctx context.Context, database *db.DB, in Input) (*Result, error) {
	if in.Name == "" {
		return nil, apperr.Validation("tenant name is required")
	}
	if in.OwnerEmail == "" {
		return nil, apperr.Validation("owner email is required")
	}

	baseSlug := tenant.Slugify(in.Name)
	if baseSlug == "" {
		return nil, apperr.Validation("tenant name %q produces an empty slug", in.Name)
	}

	migs, err := tenant.LoadTemplateMigrations()
	if err != nil {
		return nil, apperr.Internal(err)
	}

	var (
		generatedPassword string
		result            Result
	)

	txErr := database.Tx(ctx, func(tx pgx.Tx) error {
		gq := globaldb.New(tx)

		// --- Phase A: public bookkeeping (search_path = default) ---

		// Reuse an existing user (a user may own multiple tenants) or create one.
		owner, err := gq.GetUserByEmail(ctx, in.OwnerEmail)
		if errors.Is(err, pgx.ErrNoRows) {
			pw := in.OwnerPassword
			if pw == "" {
				pw, err = randomPassword()
				if err != nil {
					return apperr.Internal(err)
				}
				generatedPassword = pw
			}
			hash, err := auth.HashPassword(pw)
			if err != nil {
				return apperr.Internal(err)
			}
			owner, err = gq.CreateUser(ctx, globaldb.CreateUserParams{
				Email:        in.OwnerEmail,
				PasswordHash: hash,
				Name:         in.OwnerName,
			})
			if err != nil {
				return apperr.Internal(fmt.Errorf("create owner: %w", err))
			}
			result.OwnerCreated = true
		} else if err != nil {
			return apperr.Internal(fmt.Errorf("lookup owner: %w", err))
		}

		slug, err := freeSlug(ctx, gq, baseSlug)
		if err != nil {
			return err
		}
		schemaName := tenant.SchemaName(slug)

		code, err := nextBarcodeTenantCode(ctx, tx)
		if err != nil {
			return apperr.Internal(err)
		}

		now := pgtype.Timestamptz{Time: time.Now(), Valid: true}
		t, err := gq.CreateTenant(ctx, globaldb.CreateTenantParams{
			Name:              in.Name,
			Slug:              slug,
			SchemaName:        schemaName,
			Status:            "active",
			BarcodeTenantCode: code,
			ApprovedAt:        now,
		})
		if err != nil {
			return apperr.Internal(fmt.Errorf("create tenant row: %w", err))
		}

		if _, err := gq.CreateMembership(ctx, globaldb.CreateMembershipParams{
			UserID:   owner.ID,
			TenantID: t.ID,
			Role:     "owner",
		}); err != nil {
			return apperr.Internal(fmt.Errorf("create membership: %w", err))
		}

		if _, err := gq.UpsertTenantModule(ctx, globaldb.UpsertTenantModuleParams{
			TenantID:  t.ID,
			Module:    string(flags.ModulePIM),
			Enabled:   true,
			EnabledAt: now,
		}); err != nil {
			return apperr.Internal(fmt.Errorf("enable pim module: %w", err))
		}

		// --- Phase B: tenant schema (search_path narrowed to the new schema) ---

		if _, err := tx.Exec(ctx, "CREATE SCHEMA "+pgx.Identifier{schemaName}.Sanitize()); err != nil {
			return apperr.Internal(fmt.Errorf("create schema: %w", err))
		}
		if _, err := tx.Exec(ctx, "SET LOCAL search_path = "+pgx.Identifier{schemaName}.Sanitize()+", public"); err != nil {
			return apperr.Internal(fmt.Errorf("set search_path: %w", err))
		}

		applied, err := tenant.ApplyPending(ctx, tx, migs)
		if err != nil {
			return apperr.Internal(err)
		}
		result.AppliedMigrations = applied

		if err := seedDefaults(ctx, tx); err != nil {
			return apperr.Internal(err)
		}

		result.Tenant = t
		result.Owner = owner
		return nil
	})
	if txErr != nil {
		return nil, txErr
	}

	result.GeneratedPassword = generatedPassword
	return &result, nil
}

// DropSchema removes a tenant schema and all its objects. Used by delete-tenant
// and as a cleanup escape hatch; the happy provisioning path relies on
// transactional rollback instead.
func DropSchema(ctx context.Context, database *db.DB, schemaName string) error {
	if err := tenant.ValidateSchemaName(schemaName); err != nil {
		return err
	}
	return database.Tx(ctx, func(tx pgx.Tx) error {
		_, err := tx.Exec(ctx, "DROP SCHEMA IF EXISTS "+pgx.Identifier{schemaName}.Sanitize()+" CASCADE")
		return err
	})
}

// freeSlug returns base, or base_2, base_3, ... — the first not already taken.
func freeSlug(ctx context.Context, gq *globaldb.Queries, base string) (string, error) {
	candidate := base
	for n := 2; n < 1000; n++ {
		_, err := gq.GetTenantBySlug(ctx, candidate)
		if errors.Is(err, pgx.ErrNoRows) {
			return candidate, nil
		}
		if err != nil {
			return "", apperr.Internal(fmt.Errorf("check slug: %w", err))
		}
		candidate = fmt.Sprintf("%s_%d", base, n)
	}
	return "", apperr.Conflict("could not allocate a free slug for %q", base)
}

func nextBarcodeTenantCode(ctx context.Context, tx pgx.Tx) (int32, error) {
	var code int64
	if err := tx.QueryRow(ctx, "SELECT nextval('tenant_barcode_code_seq')").Scan(&code); err != nil {
		return 0, fmt.Errorf("next barcode tenant code: %w", err)
	}
	return int32(code), nil
}

// seedDefaults inserts the starter metaobject definitions and global attributes
// expected in every new tenant. Runs with search_path already on the schema.
func seedDefaults(ctx context.Context, tx pgx.Tx) error {
	tq := tenantdb.New(tx)

	renk, err := tq.CreateMetaobjectDefinition(ctx, tenantdb.CreateMetaobjectDefinitionParams{Key: "renk", Label: "Renk"})
	if err != nil {
		return fmt.Errorf("seed renk: %w", err)
	}
	for _, f := range []tenantdb.CreateMetaobjectFieldParams{
		{DefinitionID: renk.ID, Key: "ad", Label: "Ad", DataType: "text"},
		{DefinitionID: renk.ID, Key: "hex", Label: "Hex", DataType: "color"},
	} {
		if _, err := tq.CreateMetaobjectField(ctx, f); err != nil {
			return fmt.Errorf("seed renk field %s: %w", f.Key, err)
		}
	}

	beden, err := tq.CreateMetaobjectDefinition(ctx, tenantdb.CreateMetaobjectDefinitionParams{Key: "beden", Label: "Beden"})
	if err != nil {
		return fmt.Errorf("seed beden: %w", err)
	}
	if _, err := tq.CreateMetaobjectField(ctx, tenantdb.CreateMetaobjectFieldParams{
		DefinitionID: beden.ID, Key: "ad", Label: "Ad", DataType: "text",
	}); err != nil {
		return fmt.Errorf("seed beden field: %w", err)
	}

	for _, a := range []tenantdb.CreateAttributeParams{
		{Key: "uretici", Label: "Üretici", DataType: "text", ValueSource: "none", BindingLevel: "product", IsGlobal: true},
		{Key: "mensei", Label: "Menşei", DataType: "text", ValueSource: "none", BindingLevel: "product", IsGlobal: true},
	} {
		if _, err := tq.CreateAttribute(ctx, a); err != nil {
			return fmt.Errorf("seed attribute %s: %w", a.Key, err)
		}
	}
	return nil
}

func randomPassword() (string, error) {
	b := make([]byte, 12)
	if _, err := rand.Read(b); err != nil {
		return "", fmt.Errorf("generate password: %w", err)
	}
	return base64.RawURLEncoding.EncodeToString(b), nil
}
