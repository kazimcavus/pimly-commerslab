using Channels.Application.ExternalCatalog;
using SharedKernel;

namespace Channels.Infrastructure.Taxonomy;

internal sealed class StubMarketplaceCategoryAttributesClient : IMarketplaceCategoryAttributesClient
{
    private static readonly Dictionary<string, IReadOnlyList<MarketplaceCategoryAttributeNode>> AttributesByCategory =
        new(StringComparer.Ordinal)
        {
            ["111"] =
            [
                new MarketplaceCategoryAttributeNode(
                    "attr-renk",
                    "Renk",
                    Required: true,
                    AllowCustom: false,
                    IsVariant: true,
                    [
                        new MarketplaceAttributeValueNode("val-siyah", "Siyah"),
                        new MarketplaceAttributeValueNode("val-beyaz", "Beyaz"),
                    ],
                    IsSlicer: true),
                new MarketplaceCategoryAttributeNode(
                    "attr-hafiza",
                    "Hafıza",
                    Required: true,
                    AllowCustom: false,
                    IsVariant: false,
                    [
                        new MarketplaceAttributeValueNode("val-128", "128 GB"),
                        new MarketplaceAttributeValueNode("val-256", "256 GB"),
                    ]),
                new MarketplaceCategoryAttributeNode(
                    "attr-marka",
                    "Marka",
                    Required: false,
                    AllowCustom: true,
                    IsVariant: false,
                    []),
            ],
            ["221"] =
            [
                new MarketplaceCategoryAttributeNode(
                    "attr-beden",
                    "Beden",
                    Required: true,
                    AllowCustom: false,
                    IsVariant: true,
                    [
                        new MarketplaceAttributeValueNode("val-s", "S"),
                        new MarketplaceAttributeValueNode("val-m", "M"),
                        new MarketplaceAttributeValueNode("val-l", "L"),
                    ]),
                new MarketplaceCategoryAttributeNode(
                    "attr-renk-gomlek",
                    "Renk",
                    Required: true,
                    AllowCustom: false,
                    IsVariant: true,
                    [
                        new MarketplaceAttributeValueNode("val-mavi", "Mavi"),
                        new MarketplaceAttributeValueNode("val-beyaz-gomlek", "Beyaz"),
                    ],
                    IsSlicer: true),
                new MarketplaceCategoryAttributeNode(
                    "attr-kumas",
                    "Kumaş",
                    Required: true,
                    AllowCustom: false,
                    IsVariant: false,
                    [
                        new MarketplaceAttributeValueNode("val-pamuk", "Pamuk"),
                        new MarketplaceAttributeValueNode("val-polyester", "Polyester"),
                    ]),
            ],
        };

    public Task<Result<IReadOnlyList<MarketplaceCategoryAttributeNode>>> FetchCategoryAttributesAsync(
        Marketplace marketplace,
        string externalCategoryId,
        CancellationToken cancellationToken = default)
    {
        _ = marketplace;
        cancellationToken.ThrowIfCancellationRequested();

        if (!AttributesByCategory.TryGetValue(externalCategoryId.Trim(), out var attributes))
        {
            return Task.FromResult(Result.Success<IReadOnlyList<MarketplaceCategoryAttributeNode>>(
                Array.Empty<MarketplaceCategoryAttributeNode>()));
        }

        return Task.FromResult(Result.Success(attributes));
    }
}
