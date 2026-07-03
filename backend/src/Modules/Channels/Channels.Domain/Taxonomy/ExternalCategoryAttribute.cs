using Channels.Domain.Marketplaces;
using SharedKernel;

namespace Channels.Domain.Taxonomy;

/// <summary>Pazaryeri kategorisine ait cache'lenmiş harici attribute kaydı.</summary>
public sealed class ExternalCategoryAttribute : Entity<Guid>
{
    private ExternalCategoryAttribute()
    {
        MarketplaceKey = null!;
        ExternalCategoryId = string.Empty;
        ExternalAttributeId = string.Empty;
        Name = string.Empty;
    }

    private ExternalCategoryAttribute(
        Guid id,
        MarketplaceKey marketplaceKey,
        string externalCategoryId,
        string externalAttributeId,
        string name,
        bool required,
        bool allowCustom,
        bool isVariant,
        DateTimeOffset syncedAt)
        : base(id)
    {
        MarketplaceKey = marketplaceKey;
        ExternalCategoryId = externalCategoryId;
        ExternalAttributeId = externalAttributeId;
        Name = name;
        Required = required;
        AllowCustom = allowCustom;
        IsVariant = isVariant;
        SyncedAt = syncedAt;
    }

    public MarketplaceKey MarketplaceKey { get; private set; }

    public string ExternalCategoryId { get; private set; }

    public string ExternalAttributeId { get; private set; }

    public string Name { get; private set; }

    public bool Required { get; private set; }

    public bool AllowCustom { get; private set; }

    public bool IsVariant { get; private set; }

    public DateTimeOffset SyncedAt { get; private set; }

    public static Result<ExternalCategoryAttribute> Create(
        MarketplaceKey marketplaceKey,
        string externalCategoryId,
        string externalAttributeId,
        string name,
        bool required,
        bool allowCustom,
        bool isVariant,
        DateTimeOffset syncedAt)
    {
        if (string.IsNullOrWhiteSpace(externalCategoryId))
        {
            return Result.Failure<ExternalCategoryAttribute>(Error.Validation("External category id is required."));
        }

        if (string.IsNullOrWhiteSpace(externalAttributeId))
        {
            return Result.Failure<ExternalCategoryAttribute>(Error.Validation("External attribute id is required."));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<ExternalCategoryAttribute>(Error.Validation("External attribute name is required."));
        }

        return Result.Success(new ExternalCategoryAttribute(
            Guid.NewGuid(),
            marketplaceKey,
            externalCategoryId.Trim(),
            externalAttributeId.Trim(),
            name.Trim(),
            required,
            allowCustom,
            isVariant,
            syncedAt));
    }

    public void Update(
        string name,
        bool required,
        bool allowCustom,
        bool isVariant,
        DateTimeOffset syncedAt)
    {
        Name = name.Trim();
        Required = required;
        AllowCustom = allowCustom;
        IsVariant = isVariant;
        SyncedAt = syncedAt;
    }
}
