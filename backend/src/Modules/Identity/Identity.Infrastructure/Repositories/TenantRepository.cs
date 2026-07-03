using Identity.Domain.Tenants;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

/// <summary>Tenant aggregate'leri için EF Core tabanlı depo.</summary>
internal sealed class TenantRepository(IdentityDbContext db) : ITenantRepository
{
    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Tenants.FirstOrDefaultAsync(tenant => tenant.Id == id, cancellationToken);

    public async Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default) =>
        await db.Tenants.AddAsync(tenant, cancellationToken);
}
