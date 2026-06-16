// Command pimly is the single binary for the pimly platform: it serves the HTTP
// API and provides operational CLI subcommands (migrate, create-tenant,
// migrate-tenants).
package main

import (
	"context"
	"errors"
	"flag"
	"fmt"
	"log/slog"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	"github.com/jackc/pgx/v5"

	"github.com/kazimcavus/pimly/internal/platform/auth"
	"github.com/kazimcavus/pimly/internal/platform/config"
	"github.com/kazimcavus/pimly/internal/platform/db"
	"github.com/kazimcavus/pimly/internal/platform/db/globaldb"
	"github.com/kazimcavus/pimly/internal/platform/flags"
	"github.com/kazimcavus/pimly/internal/platform/migrate"
	"github.com/kazimcavus/pimly/internal/platform/provision"
	"github.com/kazimcavus/pimly/internal/platform/tenant"
	"github.com/kazimcavus/pimly/internal/server"
)

func main() {
	cfg, err := config.Load()
	if err != nil {
		fmt.Fprintln(os.Stderr, "config error:", err)
		os.Exit(1)
	}
	slog.SetDefault(cfg.NewLogger())

	if len(os.Args) < 2 {
		usage()
		os.Exit(2)
	}
	ctx := context.Background()
	args := os.Args[2:]
	switch os.Args[1] {
	case "migrate":
		mustRun(runMigrate(ctx, cfg))
	case "create-tenant":
		mustRun(runCreateTenant(ctx, cfg, args))
	case "migrate-tenants":
		mustRun(runMigrateTenants(ctx, cfg, args))
	case "serve":
		mustRun(runServe(ctx, cfg))
	case "help", "-h", "--help":
		usage()
	default:
		fmt.Fprintln(os.Stderr, "unknown command:", os.Args[1])
		usage()
		os.Exit(2)
	}
}

func usage() {
	fmt.Fprint(os.Stderr, `pimly — modular PIM platform

Usage:
  pimly serve                         Start the HTTP API server
  pimly migrate                       Apply global (public schema) migrations
  pimly create-tenant   [flags]       Provision a new tenant
  pimly migrate-tenants [flags]       Apply pending template migrations to all tenants

create-tenant flags:
  --name           Tenant display name (required)
  --owner-email    Owner email (required)
  --owner-name     Owner display name
  --owner-password Owner password (generated if omitted)

migrate-tenants flags:
  --dry-run        Report pending migrations without applying
  --tenant <slug>  Limit to a single tenant
`)
}

func mustRun(err error) {
	if err != nil {
		slog.Error("command failed", "err", err)
		os.Exit(1)
	}
}

// runMigrate applies the global schema migrations.
func runMigrate(_ context.Context, cfg *config.Config) error {
	if err := migrate.RunGlobal(cfg.DatabaseURL); err != nil {
		return err
	}
	slog.Info("global migrations applied")
	return nil
}

func runCreateTenant(ctx context.Context, cfg *config.Config, args []string) error {
	fs := flag.NewFlagSet("create-tenant", flag.ContinueOnError)
	name := fs.String("name", "", "tenant display name (required)")
	ownerEmail := fs.String("owner-email", "", "owner email (required)")
	ownerName := fs.String("owner-name", "", "owner display name")
	ownerPassword := fs.String("owner-password", "", "owner password (generated if omitted)")
	if err := fs.Parse(args); err != nil {
		return err
	}

	database, err := db.New(ctx, cfg.DatabaseURL, cfg.DBMaxConns, cfg.DBMinConns)
	if err != nil {
		return err
	}
	defer database.Close()

	res, err := provision.CreateTenant(ctx, database, provision.Input{
		Name:          *name,
		OwnerEmail:    *ownerEmail,
		OwnerName:     *ownerName,
		OwnerPassword: *ownerPassword,
	})
	if err != nil {
		return err
	}

	fmt.Println("✓ tenant provisioned")
	fmt.Printf("  tenant id   : %s\n", res.Tenant.ID)
	fmt.Printf("  name        : %s\n", res.Tenant.Name)
	fmt.Printf("  slug        : %s\n", res.Tenant.Slug)
	fmt.Printf("  schema      : %s\n", res.Tenant.SchemaName)
	fmt.Printf("  barcode code: %04d\n", res.Tenant.BarcodeTenantCode)
	fmt.Printf("  owner       : %s (%s)\n", res.Owner.Email, res.Owner.Name)
	fmt.Printf("  migrations  : %d applied\n", res.AppliedMigrations)
	if res.GeneratedPassword != "" {
		fmt.Printf("  password    : %s   (generated — store it now)\n", res.GeneratedPassword)
	} else if !res.OwnerCreated {
		fmt.Printf("  password    : (existing user — unchanged)\n")
	}
	return nil
}

func runMigrateTenants(ctx context.Context, cfg *config.Config, args []string) error {
	fs := flag.NewFlagSet("migrate-tenants", flag.ContinueOnError)
	dryRun := fs.Bool("dry-run", false, "report without applying")
	only := fs.String("tenant", "", "limit to a single tenant slug")
	if err := fs.Parse(args); err != nil {
		return err
	}

	database, err := db.New(ctx, cfg.DatabaseURL, cfg.DBMaxConns, cfg.DBMinConns)
	if err != nil {
		return err
	}
	defer database.Close()

	migs, err := tenant.LoadTemplateMigrations()
	if err != nil {
		return err
	}
	latest := tenant.LatestTemplateVersion(migs)

	tenants, err := globaldb.New(database.Pool()).ListTenants(ctx)
	if err != nil {
		return fmt.Errorf("list tenants: %w", err)
	}

	fmt.Printf("template latest version: %d (dry-run=%v)\n", latest, *dryRun)
	for _, t := range tenants {
		if *only != "" && t.Slug != *only {
			continue
		}
		from, to, applied, err := migrateOneTenant(ctx, database, t.SchemaName, migs, *dryRun)
		if err != nil {
			fmt.Printf("  ✗ %-24s error: %v\n", t.Slug, err)
			continue
		}
		if *dryRun {
			pending := latest - from
			if pending < 0 {
				pending = 0
			}
			fmt.Printf("  • %-24s v%d (%d pending)\n", t.Slug, from, pending)
		} else {
			fmt.Printf("  ✓ %-24s v%d → v%d (%d applied)\n", t.Slug, from, to, applied)
		}
	}
	return nil
}

// migrateOneTenant applies (or, for dry-run, just inspects) pending template
// migrations for a single tenant within its own transaction. A per-schema
// advisory lock prevents concurrent runs from double-applying.
func migrateOneTenant(ctx context.Context, database *db.DB, schema string, migs []tenant.TemplateMigration, dryRun bool) (from, to, applied int, err error) {
	errDryRun := errors.New("dry-run rollback")
	wErr := database.WithTenant(ctx, schema, func(tx pgx.Tx) error {
		// Transaction-scoped advisory lock; auto-released at COMMIT/ROLLBACK.
		if _, e := tx.Exec(ctx, "SELECT pg_advisory_xact_lock(hashtext($1))", schema); e != nil {
			return e
		}
		cur, e := tenant.CurrentSchemaVersion(ctx, tx)
		if e != nil {
			return e
		}
		from = cur
		if dryRun {
			to = cur
			return errDryRun // roll back so dry-run leaves no side effects
		}
		n, e := tenant.ApplyPending(ctx, tx, migs)
		if e != nil {
			return e
		}
		applied = n
		newVer, e := tenant.CurrentSchemaVersion(ctx, tx)
		if e != nil {
			return e
		}
		to = newVer
		return nil
	})
	if wErr != nil && !errors.Is(wErr, errDryRun) {
		err = wErr
	}
	return
}

func runServe(ctx context.Context, cfg *config.Config) error {
	database, err := db.New(ctx, cfg.DatabaseURL, cfg.DBMaxConns, cfg.DBMinConns)
	if err != nil {
		return err
	}
	defer database.Close()

	secret := cfg.JWTSecret
	if secret == "" {
		secret = "pimly-insecure-dev-secret"
		slog.Warn("PIMLY_JWT_SECRET is not set; using an insecure dev secret")
	}
	authService := auth.NewService(database, secret, cfg.JWTTTL)
	handler := server.New(server.Deps{
		DB:    database,
		Auth:  authService,
		Flags: flags.AlwaysOn{},
	})

	srv := &http.Server{
		Addr:              cfg.HTTPAddr,
		Handler:           handler,
		ReadHeaderTimeout: 10 * time.Second,
	}

	go func() {
		slog.Info("pimly listening", "addr", cfg.HTTPAddr)
		if err := srv.ListenAndServe(); err != nil && !errors.Is(err, http.ErrServerClosed) {
			slog.Error("server error", "err", err)
		}
	}()

	stop := make(chan os.Signal, 1)
	signal.Notify(stop, syscall.SIGINT, syscall.SIGTERM)
	<-stop
	slog.Info("shutting down")
	shutdownCtx, cancel := context.WithTimeout(ctx, 10*time.Second)
	defer cancel()
	return srv.Shutdown(shutdownCtx)
}
