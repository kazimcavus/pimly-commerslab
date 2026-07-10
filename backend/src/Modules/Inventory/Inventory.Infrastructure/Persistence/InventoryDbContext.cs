using Inventory.Domain;
using Inventory.Domain.StockLevels;
using Inventory.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Tenancy;

namespace Inventory.Infrastructure.Persistence;

/// <summary>Inventory modülü için Entity Framework veritabanı bağlamı.</summary>
public sealed class InventoryDbContext : DbContext, IUnitOfWork
{
    private readonly ITenantContext? _tenantContext;

    public InventoryDbContext(DbContextOptions<InventoryDbContext> options)
        : this(options, null)
    {
    }

    public InventoryDbContext(DbContextOptions<InventoryDbContext> options, ITenantContext? tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    /// <summary>Gets kalem stok kayıtları kümesi.</summary>
    public DbSet<StockLevel> StockLevels => Set<StockLevel>();

    internal Guid CurrentTenantId =>
        _tenantContext?.TenantId
        ?? throw new InvalidOperationException("Tenant id is not available in the current HTTP context.");

    // Model cache anahtarı ve query filter kurulumu için fırlatmayan tenant erişimi.
    // HTTP dışı bağlamlarda (migration, design-time) Guid.Empty döner.
    internal Guid ModelCacheTenantId
    {
        get
        {
            try
            {
                return _tenantContext?.TenantId ?? Guid.Empty;
            }
            catch (InvalidOperationException)
            {
                return Guid.Empty;
            }
        }
    }

    /// <inheritdoc/>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_tenantContext is not null)
        {
            this.StampTenantId(CurrentTenantId);
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("inventory");

        modelBuilder.ApplyInventoryTenancy(ModelCacheTenantId);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
    }
}
