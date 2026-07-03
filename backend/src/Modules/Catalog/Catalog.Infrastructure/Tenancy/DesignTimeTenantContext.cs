using SharedKernel.Tenancy;

namespace Catalog.Infrastructure.Tenancy;

/// <summary>EF design-time ve migration araçları için boş tenant bağlamı.</summary>
internal sealed class DesignTimeTenantContext : ITenantContext
{
    public Guid TenantId => Guid.Empty;
}
