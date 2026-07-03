namespace SharedKernel.Tenancy;

/// <summary>
/// HTTP dışı akışlar (worker, arka plan işi) için elle set edilen tenant bağlamı.
/// Set edilmeden okunursa <see cref="Guid.Empty"/> döner (fırlatmaz); böylece
/// design-time/migration model kurulumları güvenle çalışır.
/// </summary>
public sealed class AmbientTenantContext : ITenantContext
{
    private Guid _tenantId;

    public Guid TenantId => _tenantId;

    /// <summary>Scope için tenant kimliğini bir kez set eder; farklı bir tenant ile tekrar set edilemez.</summary>
    public void Set(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id cannot be empty.", nameof(tenantId));
        }

        if (_tenantId != Guid.Empty && _tenantId != tenantId)
        {
            throw new InvalidOperationException("Tenant context is already set for this scope.");
        }

        _tenantId = tenantId;
    }
}
