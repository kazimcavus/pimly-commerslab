using Channels.Domain;
using Channels.Domain.Connections;
using Channels.Domain.Taxonomy;
using Channels.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Tenancy;

namespace Channels.Infrastructure.Persistence;

/// <summary>Channels modülü için Entity Framework veritabanı bağlamı.</summary>
public sealed class ChannelsDbContext : DbContext, IUnitOfWork
{
    private readonly ITenantContext? _tenantContext;

    public ChannelsDbContext(DbContextOptions<ChannelsDbContext> options)
        : this(options, null)
    {
    }

    public ChannelsDbContext(DbContextOptions<ChannelsDbContext> options, ITenantContext? tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<MarketplaceConnection> MarketplaceConnections => Set<MarketplaceConnection>();

    public DbSet<TaxonomySyncRun> TaxonomySyncRuns => Set<TaxonomySyncRun>();

    public DbSet<ExternalCategory> ExternalCategories => Set<ExternalCategory>();

    public DbSet<CategoryChannelMapping> CategoryChannelMappings => Set<CategoryChannelMapping>();

    public DbSet<ExternalCategoryAttribute> ExternalCategoryAttributes => Set<ExternalCategoryAttribute>();

    public DbSet<ExternalAttributeValue> ExternalAttributeValues => Set<ExternalAttributeValue>();

    public DbSet<AttributeChannelMapping> AttributeChannelMappings => Set<AttributeChannelMapping>();

    public DbSet<AttributeValueChannelMapping> AttributeValueChannelMappings => Set<AttributeValueChannelMapping>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("channels");

        var tenantId = _tenantContext?.TenantId ?? Guid.Empty;
        modelBuilder.ApplyChannelsTenancy(tenantId);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChannelsDbContext).Assembly);
    }
}
