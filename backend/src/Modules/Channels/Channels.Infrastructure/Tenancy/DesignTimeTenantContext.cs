using SharedKernel.Tenancy;

namespace Channels.Infrastructure.Tenancy;

/// <summary>EF design-time ve worker için boş tenant bağlamı.</summary>
internal sealed class DesignTimeTenantContext : ITenantContext
{
    public Guid TenantId => Guid.Empty;
}
