using Channels.Domain.ExternalCatalog;
using Channels.Domain.Marketplaces;
using SharedKernel;

namespace Channels.Application.ExternalCatalog;

/// <summary>
/// Pazaryerinden kategori attribute'larını çekip yerel cache'e upsert eden ortak destek.
/// Hem eşleme UI'ının canlı listelemesi hem de ürün import hattı tarafından kullanılır.
/// SaveChanges çağıranın sorumluluğundadır.
/// </summary>
public static class ExternalCategoryAttributeCacheSupport
{
    /// <summary>Attribute'ları pazaryerinden çekip cache'e yazar (IsVariant/IsSlicer dahil).</summary>
    public static async Task<Result> RefreshAsync(
        IMarketplaceCategoryAttributesClient attributesClient,
        IExternalCategoryAttributeRepository externalAttributes,
        Marketplace marketplace,
        string externalCategoryId,
        DateTimeOffset syncedAt,
        CancellationToken cancellationToken = default)
    {
        var fetchResult = await attributesClient.FetchCategoryAttributesAsync(
            marketplace,
            externalCategoryId,
            cancellationToken);

        if (fetchResult.IsFailure)
        {
            return Result.Failure(fetchResult.Error);
        }

        var upserts = fetchResult.Value
            .Select(attribute => new ExternalCategoryAttributeUpsert(
                attribute.ExternalAttributeId,
                attribute.Name,
                attribute.Required,
                attribute.AllowCustom,
                attribute.IsVariant,
                attribute.Values
                    .Select(value => new ExternalAttributeValueUpsert(value.ExternalValueId, value.Name))
                    .ToList(),
                attribute.IsSlicer))
            .ToList();

        await externalAttributes.UpsertBatchAsync(
            marketplace,
            externalCategoryId,
            upserts,
            syncedAt,
            cancellationToken);

        return Result.Success();
    }
}
