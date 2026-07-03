using Channels.Domain.Marketplaces;
using SharedKernel;

namespace Channels.Domain.CategoryChannelMappings;

/// <summary>Catalog kategorisi ile pazaryeri harici kategorisi arasındaki eşleme.</summary>
public sealed class CategoryChannelMapping : AggregateRoot<Guid>
{
    private CategoryChannelMapping()
    {
        Marketplace = null!;
        ExternalId = string.Empty;
    }

    private CategoryChannelMapping(
        Guid id,
        Guid tenantId,
        Guid catalogCategoryId,
        Marketplace marketplace,
        string externalId)
        : base(id)
    {
        TenantId = tenantId;
        CatalogCategoryId = catalogCategoryId;
        Marketplace = marketplace;
        ExternalId = externalId;
    }

    /// <summary>Gets tenant kimliği.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets catalog kategori kimliği.</summary>
    public Guid CatalogCategoryId { get; private set; }

    /// <summary>Gets pazaryeri anahtarı.</summary>
    public Marketplace Marketplace { get; private set; }

    /// <summary>Gets pazaryeri harici kategori kimliği.</summary>
    public string ExternalId { get; private set; }

    /// <summary>Yeni kategori eşlemesi oluşturur.</summary>
    public static Result<CategoryChannelMapping> Create(
        Guid tenantId,
        Guid catalogCategoryId,
        Marketplace marketplace,
        string externalId)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure<CategoryChannelMapping>(Error.Validation("Tenant id is required."));
        }

        if (catalogCategoryId == Guid.Empty)
        {
            return Result.Failure<CategoryChannelMapping>(Error.Validation("Catalog category id is required."));
        }

        if (string.IsNullOrWhiteSpace(externalId))
        {
            return Result.Failure<CategoryChannelMapping>(Error.Validation("External category id is required."));
        }

        return Result.Success(new CategoryChannelMapping(
            Guid.NewGuid(),
            tenantId,
            catalogCategoryId,
            marketplace,
            externalId.Trim()));
    }

    /// <summary>Harici kategori referansını günceller.</summary>
    public Result UpdateExternalCategory(string externalId)
    {
        if (string.IsNullOrWhiteSpace(externalId))
        {
            return Result.Failure(Error.Validation("External category id is required."));
        }

        ExternalId = externalId.Trim();
        return Result.Success();
    }
}
