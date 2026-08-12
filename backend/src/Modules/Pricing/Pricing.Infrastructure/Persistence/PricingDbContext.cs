using Microsoft.EntityFrameworkCore;
using Pimly.Outbox;
using Pricing.Domain;
using Pricing.Domain.BasePrices;
using Pricing.Domain.ChannelPrices;
using Pricing.Domain.ItemPrices;
using Pricing.Domain.PriceDefinitions;
using Pricing.Infrastructure.Tenancy;
using SharedKernel.Tenancy;

namespace Pricing.Infrastructure.Persistence;

/// <summary>Pricing modülü için Entity Framework veritabanı bağlamı.</summary>
public sealed class PricingDbContext : DbContext, IUnitOfWork, IOutboxDbContext
{
    private readonly ITenantContext? _tenantContext;

    public PricingDbContext(DbContextOptions<PricingDbContext> options)
        : this(options, null)
    {
    }

    public PricingDbContext(DbContextOptions<PricingDbContext> options, ITenantContext? tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    /// <summary>Gets fiyat tanımları kümesi.</summary>
    public DbSet<PriceDefinition> PriceDefinitions => Set<PriceDefinition>();

    /// <summary>Gets kalem fiyatları kümesi.</summary>
    public DbSet<ProductItemPrice> ProductItemPrices => Set<ProductItemPrice>();

    /// <summary>Gets kalem temel fiyatları kümesi.</summary>
    public DbSet<BasePrice> BasePrices => Set<BasePrice>();

    /// <summary>Gets kalem kanal (pazaryeri) fiyatları kümesi.</summary>
    public DbSet<ChannelPrice> ChannelPrices => Set<ChannelPrice>();

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
        modelBuilder.HasDefaultSchema("pricing");

        modelBuilder.ApplyPricingTenancy(ModelCacheTenantId);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PricingDbContext).Assembly);

        modelBuilder.AddOutbox();
    }
}
