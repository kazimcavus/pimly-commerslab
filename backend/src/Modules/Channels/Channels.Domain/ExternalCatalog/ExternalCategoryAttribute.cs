using Channels.Domain.Marketplaces;
using SharedKernel;

namespace Channels.Domain.ExternalCatalog;

/// <summary>Pazaryeri kategorisine ait cache'lenmiş harici attribute kaydı.</summary>
public sealed class ExternalCategoryAttribute : Entity<Guid>
{
    private ExternalCategoryAttribute()
    {
        Marketplace = null!;
        ExternalCategoryId = string.Empty;
        ExternalAttributeId = string.Empty;
        Name = string.Empty;
    }

    private ExternalCategoryAttribute(
        Guid id,
        Marketplace marketplace,
        string externalCategoryId,
        string externalAttributeId,
        string name,
        bool required,
        bool allowCustom,
        bool isVariant,
        bool isSlicer,
        DateTimeOffset syncedAt)
        : base(id)
    {
        Marketplace = marketplace;
        ExternalCategoryId = externalCategoryId;
        ExternalAttributeId = externalAttributeId;
        Name = name;
        Required = required;
        AllowCustom = allowCustom;
        IsVariant = isVariant;
        IsSlicer = isSlicer;
        SyncedAt = syncedAt;
    }

    public Marketplace Marketplace { get; private set; }

    public string ExternalCategoryId { get; private set; }

    public string ExternalAttributeId { get; private set; }

    public string Name { get; private set; }

    public bool Required { get; private set; }

    public bool AllowCustom { get; private set; }

    public bool IsVariant { get; private set; }

    public bool IsSlicer { get; private set; }

    public DateTimeOffset SyncedAt { get; private set; }

    public static Result<ExternalCategoryAttribute> Create(
        Marketplace marketplace,
        string externalCategoryId,
        string externalAttributeId,
        string name,
        bool required,
        bool allowCustom,
        bool isVariant,
        bool isSlicer,
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
            marketplace,
            externalCategoryId.Trim(),
            externalAttributeId.Trim(),
            name.Trim(),
            required,
            allowCustom,
            isVariant,
            isSlicer,
            syncedAt));
    }

    public void Update(
        string name,
        bool required,
        bool allowCustom,
        bool isVariant,
        bool isSlicer,
        DateTimeOffset syncedAt)
    {
        Name = name.Trim();
        Required = required;
        AllowCustom = allowCustom;
        IsVariant = isVariant;
        IsSlicer = isSlicer;
        SyncedAt = syncedAt;
    }
}
