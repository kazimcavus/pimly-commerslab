using Inventory.Domain;
using Inventory.Domain.StockLevels;
using Inventory.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Pimly.Outbox;
using SharedKernel.Tenancy;

namespace Inventory.Infrastructure.Persistence;

/// <summary>Inventory modülü için Entity Framework veritabanı bağlamı.</summary>
public sealed class InventoryDbContext : DbContext, IUnitOfWork, IOutboxDbContext
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

    /// <summary>Gets modülün outbox kayıtları kümesi.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

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
            // Tenant yalnızca tenant-kapsamlı EKLEME veya integration olay varsa zorunludur.
            // Outbox'ı "işlendi" işaretleyen tenant'sız kaydetmeler (dispatcher üst scope'u) serbest
            // geçer; StampTenantId ve WriteOutboxMessages boş tenant'ta ekleme/olay bulursa hata verir.
            var tenantId = ModelCacheTenantId;
            this.StampTenantId(tenantId);
            this.WriteOutboxMessages(tenantId);
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("inventory");

        modelBuilder.ApplyInventoryTenancy(ModelCacheTenantId);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);

        modelBuilder.AddOutbox();
    }
}
