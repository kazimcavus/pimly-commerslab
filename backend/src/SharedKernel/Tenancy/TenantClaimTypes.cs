namespace SharedKernel.Tenancy;

/// <summary>Tenant ile ilgili JWT claim adları.</summary>
public static class TenantClaimTypes
{
    /// <summary>Tenant kimliği claim'i (snake_case, API ile uyumlu).</summary>
    public const string TenantId = "tenant_id";
}
