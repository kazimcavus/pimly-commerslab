using Channels.Domain.Marketplaces;

namespace Channels.Domain.Taxonomy;

/// <summary>ExternalCategoryAttribute cache depo arabirimi.</summary>
public interface IExternalCategoryAttributeRepository
{
    Task<ExternalCategoryAttribute?> GetAsync(
        MarketplaceKey marketplaceKey,
        string externalCategoryId,
        string externalAttributeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExternalCategoryAttribute>> ListByCategoryAsync(
        MarketplaceKey marketplaceKey,
        string externalCategoryId,
        CancellationToken cancellationToken = default);

    Task UpsertBatchAsync(
        MarketplaceKey marketplaceKey,
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
    IReadOnlyList<ExternalAttributeValueUpsert> Values);

/// <summary>Harici attribute değer upsert girdisi.</summary>
public sealed record ExternalAttributeValueUpsert(
    string ExternalValueId,
    string Name);
