using Channels.Domain.Marketplaces;

namespace Channels.Domain.Taxonomy;

/// <summary>ExternalAttributeValue cache depo arabirimi.</summary>
public interface IExternalAttributeValueRepository
{
    Task<ExternalAttributeValue?> GetAsync(
        MarketplaceKey marketplaceKey,
        string externalCategoryId,
        string externalAttributeId,
        string externalValueId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExternalAttributeValue>> ListByAttributeAsync(
        MarketplaceKey marketplaceKey,
        string externalCategoryId,
        string externalAttributeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExternalAttributeValue>> ListByCategoryAsync(
        MarketplaceKey marketplaceKey,
        string externalCategoryId,
        CancellationToken cancellationToken = default);
}
