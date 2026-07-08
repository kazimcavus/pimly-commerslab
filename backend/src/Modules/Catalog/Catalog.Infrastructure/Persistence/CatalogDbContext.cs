using System.Text.Json;
using Catalog.Domain;
using Catalog.Domain.Barcodes;
using Catalog.Domain.Brands;
using Catalog.Domain.Categories;
using Catalog.Domain.PriceDefinitions;
using Catalog.Domain.Products;
using Catalog.Domain.Settings;
using Catalog.Domain.SkuGenerator;
using Catalog.Domain.Variants;
using Catalog.Infrastructure.Outbox;
using Catalog.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Tenancy;
using CatalogVariant = Catalog.Domain.Variants.Variant;
using DomainAttribute = Catalog.Domain.Attributes.Attribute;

namespace Catalog.Infrastructure.Persistence;

/// <summary>Catalog modülü için Entity Framework veritabanı bağlamı.</summary>
public sealed class CatalogDbContext : DbContext, IUnitOfWork
{
    private readonly ITenantContext? _tenantContext;

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options)
        : this(options, null)
    {
    }

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options, ITenantContext? tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

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

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Brand> Brands => Set<Brand>();

    public DbSet<DomainAttribute> Attributes => Set<DomainAttribute>();

    public DbSet<CatalogVariant> Variants => Set<CatalogVariant>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductItem> ProductItems => Set<ProductItem>();

    public DbSet<PriceDefinition> PriceDefinitions => Set<PriceDefinition>();

    public DbSet<ProductItemPrice> ProductItemPrices => Set<ProductItemPrice>();

    public DbSet<BarcodeSequence> BarcodeSequences => Set<BarcodeSequence>();

    public DbSet<BarcodeAllocation> BarcodeAllocations => Set<BarcodeAllocation>();

    public DbSet<SkuGeneratorConfig> SkuGeneratorConfigs => Set<SkuGeneratorConfig>();

    public DbSet<CatalogSettings> CatalogSettings => Set<CatalogSettings>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc/>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_tenantContext is not null)
        {
            this.StampTenantId(CurrentTenantId);
            WriteOutboxMessages(CurrentTenantId);
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    // Integration olaylarını aggregate değişiklikleriyle aynı transaction'da outbox'a yazar.
    private void WriteOutboxMessages(Guid tenantId)
    {
        var holders = ChangeTracker.Entries()
            .Select(entry => entry.Entity)
            .OfType<IHasDomainEvents>()
            .Where(holder => holder.DomainEvents.Count > 0)
            .ToList();

        if (holders.Count == 0)
        {
            return;
        }

        var messages = new List<OutboxMessage>();
        foreach (var holder in holders)
        {
            foreach (var integrationEvent in holder.DomainEvents.OfType<IntegrationEvent>())
            {
                var eventType = integrationEvent.GetType();
                messages.Add(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Type = eventType.FullName!,
                    Payload = JsonSerializer.Serialize(integrationEvent, eventType, OutboxSerialization.JsonOptions),
                    OccurredOnUtc = integrationEvent.OccurredOnUtc,
                });
            }

            holder.ClearDomainEvents();
        }

        if (messages.Count > 0)
        {
            OutboxMessages.AddRange(messages);
        }
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("catalog");

        modelBuilder.ApplyCatalogTenancy(ModelCacheTenantId);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
