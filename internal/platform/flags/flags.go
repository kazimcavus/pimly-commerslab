// Package flags gates optional modules per tenant. M1 ships an always-on stub;
// M6 replaces it with a DB-backed checker reading public.tenant_modules.
package flags

import (
	"context"

	"github.com/google/uuid"
)

// Module identifies a gateable platform module.
type Module string

const (
	ModulePIM         Module = "pim"
	ModuleIntegration Module = "integration"
	ModuleWMS         Module = "wms"
)

// Checker reports whether a module is enabled for a tenant.
type Checker interface {
	Enabled(ctx context.Context, tenantID uuid.UUID, module Module) (bool, error)
}

// AlwaysOn is the M1 stub: every module is considered enabled.
type AlwaysOn struct{}

func (AlwaysOn) Enabled(context.Context, uuid.UUID, Module) (bool, error) { return true, nil }
