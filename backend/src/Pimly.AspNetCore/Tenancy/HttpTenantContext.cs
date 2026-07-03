using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SharedKernel.Tenancy;

namespace Pimly.AspNetCore.Tenancy;

/// <summary>JWT claim'lerinden tenant kimliğini çözen HTTP bağlamı implementasyonu.</summary>
internal sealed class HttpTenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    /// <inheritdoc/>
    public Guid TenantId
    {
        get
        {
            if (TryGetTenantId(out var tenantId))
            {
                return tenantId;
            }

            throw new InvalidOperationException("Tenant id is not available in the current HTTP context.");
        }
    }

    private bool TryGetTenantId(out Guid tenantId)
    {
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            tenantId = default;
            return false;
        }

        var tenantClaim = principal.FindFirstValue(TenantClaimTypes.TenantId);
        return Guid.TryParse(tenantClaim, out tenantId);
    }
}
