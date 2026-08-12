using Channels.Domain;
using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.Connections;
using Channels.Domain.ExternalCatalog;
using Channels.Domain.Listings;
using Channels.Domain.ProductImports;
using Channels.Domain.Publications;
using Channels.Domain.TaxonomySync;
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

    public DbSet<MarketplaceConnection> MarketplaceConnections => Set<MarketplaceConnection>();

    public DbSet<TaxonomySyncRun> TaxonomySyncRuns => Set<TaxonomySyncRun>();

    public DbSet<ProductImportRun> ProductImportRuns => Set<ProductImportRun>();

    public DbSet<ProductPublicationRun> ProductPublicationRuns => Set<ProductPublicationRun>();

    public DbSet<ProductListing> ProductListings => Set<ProductListing>();

    public DbSet<ExternalCategory> ExternalCategories => Set<ExternalCategory>();

    public DbSet<CategoryChannelMapping> CategoryChannelMappings => Set<CategoryChannelMapping>();

    public DbSet<ExternalCategoryAttribute> ExternalCategoryAttributes => Set<ExternalCategoryAttribute>();

    public DbSet<ExternalAttributeValue> ExternalAttributeValues => Set<ExternalAttributeValue>();

    public DbSet<AttributeChannelMapping> AttributeChannelMappings => Set<AttributeChannelMapping>();

    public DbSet<AttributeValueChannelMapping> AttributeValueChannelMappings => Set<AttributeValueChannelMapping>();

    /// <inheritdoc/>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // ProductImportError, app-assigned Guid anahtarına sahip owned bir child'dır; EF onu
        // izlenen aggregate'e eklendiğinde "var olan kayıt" sanıp Modified işaretler → UPDATE
        // 0 satır → DbUpdateConcurrencyException. Hata kayıtları yalnızca eklenir (hiç
        // güncellenmez), bu yüzden Modified görülen her kaydı güvenle Added'e çeviririz.
        foreach (var entry in ChangeTracker.Entries<ProductImportError>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.State = EntityState.Added;
            }
        }

        // ProductPublicationError de append-only owned child'dır; aynı Modified→Added düzeltmesi.
        foreach (var entry in ChangeTracker.Entries<ProductPublicationError>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.State = EntityState.Added;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("channels");

        modelBuilder.ApplyChannelsTenancy(ModelCacheTenantId);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChannelsDbContext).Assembly);
    }
}
