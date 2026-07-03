namespace Identity.Domain.Tenants;

/// <summary>TenantMembership depo arabirimi.</summary>
public interface ITenantMembershipRepository
{
    Task<TenantMembership?> GetPrimaryForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(TenantMembership membership, CancellationToken cancellationToken = default);
}
