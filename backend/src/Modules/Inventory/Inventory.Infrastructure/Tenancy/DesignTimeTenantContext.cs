using SharedKernel.Tenancy;

namespace Inventory.Infrastructure.Tenancy;

/// <summary>EF design-time ve migration araçları için boş tenant bağlamı.</summary>
internal sealed class DesignTimeTenantContext : ITenantContext
{
    public Guid TenantId => Guid.Empty;
}
