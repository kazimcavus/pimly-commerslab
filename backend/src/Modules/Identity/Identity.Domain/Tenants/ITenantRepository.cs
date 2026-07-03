namespace Identity.Domain.Tenants;

/// <summary>Tenant aggregate depo arabirimi.</summary>
public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default);
}
