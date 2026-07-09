using SharedKernel;

namespace Channels.Domain.ExternalCatalog;

/// <summary>ExternalCategoryAttribute cache depo arabirimi.</summary>
public interface IExternalCategoryAttributeRepository
{
    Task<ExternalCategoryAttribute?> GetAsync(
        Marketplace marketplace,
        string externalCategoryId,
        string externalAttributeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExternalCategoryAttribute>> ListByCategoryAsync(
        Marketplace marketplace,
        string externalCategoryId,
        CancellationToken cancellationToken = default);

    Task UpsertBatchAsync(
        Marketplace marketplace,
        string externalCategoryId,
        IReadOnlyList<ExternalCategoryAttributeUpsert> attributes,
        DateTimeOffset syncedAt,
        CancellationToken cancellationToken = default);
}

/// <summary>Harici kategori attribute upsert girdisi.</summary>
public sealed record ExternalCategoryAttributeUpsert(
    string ExternalAttributeId,
    string Name,
    bool Required,
    bool AllowCustom,
    bool IsVariant,
    IReadOnlyList<ExternalAttributeValueUpsert> Values,
    bool IsSlicer = false);

/// <summary>Harici attribute değer upsert girdisi.</summary>
public sealed record ExternalAttributeValueUpsert(
    string ExternalValueId,
    string Name);
