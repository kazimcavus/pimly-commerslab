using Channels.Domain.Marketplaces;
using SharedKernel;

namespace Channels.Domain.Taxonomy;

/// <summary>Pazaryeri attribute değeri cache kaydı.</summary>
public sealed class ExternalAttributeValue : Entity<Guid>
{
    private ExternalAttributeValue()
    {
        MarketplaceKey = null!;
        ExternalCategoryId = string.Empty;
        ExternalAttributeId = string.Empty;
        ExternalValueId = string.Empty;
        Name = string.Empty;
    }

    private ExternalAttributeValue(
        Guid id,
        MarketplaceKey marketplaceKey,
        string externalCategoryId,
        string externalAttributeId,
        string externalValueId,
        string name,
        DateTimeOffset syncedAt)
        : base(id)
    {
        MarketplaceKey = marketplaceKey;
        ExternalCategoryId = externalCategoryId;
        ExternalAttributeId = externalAttributeId;
        ExternalValueId = externalValueId;
        Name = name;
        SyncedAt = syncedAt;
    }

    public MarketplaceKey MarketplaceKey { get; private set; }

    public string ExternalCategoryId { get; private set; }

    public string ExternalAttributeId { get; private set; }

    public string ExternalValueId { get; private set; }

    public string Name { get; private set; }

    public DateTimeOffset SyncedAt { get; private set; }

    public static Result<ExternalAttributeValue> Create(
        MarketplaceKey marketplaceKey,
        string externalCategoryId,
        string externalAttributeId,
        string externalValueId,
        string name,
        DateTimeOffset syncedAt)
    {
        if (string.IsNullOrWhiteSpace(externalCategoryId))
        {
            return Result.Failure<ExternalAttributeValue>(Error.Validation("External category id is required."));
        }

        if (string.IsNullOrWhiteSpace(externalAttributeId))
        {
            return Result.Failure<ExternalAttributeValue>(Error.Validation("External attribute id is required."));
        }

        if (string.IsNullOrWhiteSpace(externalValueId))
        {
            return Result.Failure<ExternalAttributeValue>(Error.Validation("External value id is required."));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<ExternalAttributeValue>(Error.Validation("External value name is required."));
        }

        return Result.Success(new ExternalAttributeValue(
            Guid.NewGuid(),
            marketplaceKey,
            externalCategoryId.Trim(),
            externalAttributeId.Trim(),
            externalValueId.Trim(),
            name.Trim(),
            syncedAt));
    }

    public void Update(string name, DateTimeOffset syncedAt)
    {
        Name = name.Trim();
        SyncedAt = syncedAt;
    }
}
