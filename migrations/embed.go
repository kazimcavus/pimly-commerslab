// Package migrations embeds the SQL migration files so the binary is
// self-contained: the global schema (golang-migrate, via iofs) and the tenant
// template (applied programmatically by the tenant migration runner).
package migrations

import "embed"

// GlobalFS holds the public-schema golang-migrate files (global/*.sql).
//
//go:embed global/*.sql
var GlobalFS embed.FS

// TenantTemplateFS holds the per-tenant template files (tenant_template/*.sql).
//
//go:embed tenant_template/*.sql
var TenantTemplateFS embed.FS
