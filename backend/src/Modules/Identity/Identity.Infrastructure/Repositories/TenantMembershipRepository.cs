using Identity.Domain.Tenants;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

/// <summary>TenantMembership kayıtları için EF Core tabanlı depo.</summary>
internal sealed class TenantMembershipRepository(IdentityDbContext db) : ITenantMembershipRepository
{
    public Task<TenantMembership?> GetPrimaryForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        db.TenantMemberships
            .Where(membership => membership.UserId == userId && membership.IsPrimary)
            .OrderByDescending(membership => membership.JoinedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(TenantMembership membership, CancellationToken cancellationToken = default) =>
        await db.TenantMemberships.AddAsync(membership, cancellationToken);
}
