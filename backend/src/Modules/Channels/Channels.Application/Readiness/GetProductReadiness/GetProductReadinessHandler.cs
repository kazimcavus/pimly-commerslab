using Channels.Application.Contracts;
using Channels.Application.Listings.ContentSync;
using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.Connections;
using Channels.Domain.ExternalCatalog;
using SharedKernel;

namespace Channels.Application.Readiness.GetProductReadiness;

/// <summary>
/// Ürünün bağlı her pazaryeri için yayına hazır olup olmadığını hesaplar: kategori eşlemesi,
/// pazaryerinin zorunlu tuttuğu özelliklerin eşleme + değer durumu ve kalem barkodları.
/// </summary>
/// <remarks>
/// Zorunluluk kaynağı pazaryerinin kendi kategori şeması cache'idir (external_category_attributes);
/// PIM kategori atamasındaki Required alanı burada kullanılmaz. Bu sayede "başka sistemin
/// zorunlusu" bir kanalın hazırlığını etkilemez, her kanal kendi gereksinimini görür.
/// </remarks>
public sealed class GetProductReadinessHandler(
    ICatalogListingSourceGateway catalogSource,
    IMarketplaceConnectionRepository connections,
    ICategoryChannelMappingRepository categoryMappings,
    IExternalCategoryAttributeRepository externalAttributes,
    IAttributeChannelMappingRepository attributeMappings) : IGetProductReadinessHandler
{
    // Kategori başına eşleme sayısı pratikte küçüktür; tek sayfada okunur.
    private static readonly Pagination AllMappings = new(1, Pagination.MaxPageSize);

    /// <inheritdoc/>
    public async Task<Result<ProductReadinessDto>> ExecuteAsync(
        GetProductReadinessQuery query,
        CancellationToken cancellationToken = default)
    {
        var sources = await catalogSource.GetByProductAsync(query.ProductId, cancellationToken);
        if (sources.Count == 0)
        {
            return Result.Failure<ProductReadinessDto>(
                Error.NotFound("Ürün bulunamadı veya satılabilir kalemi yok."));
        }

        var categoryId = sources[0].CategoryId;
        var configured = await connections.GetConfiguredMarketplacesAsync(cancellationToken);

        var channels = new List<ChannelReadinessDto>();
        foreach (var marketplace in configured.OrderBy(m => m.Code, StringComparer.Ordinal))
        {
            channels.Add(await BuildChannelReadinessAsync(marketplace, categoryId, sources, cancellationToken));
        }

        return Result.Success(new ProductReadinessDto(query.ProductId, channels));
    }

    private async Task<ChannelReadinessDto> BuildChannelReadinessAsync(
        Marketplace marketplace,
        Guid categoryId,
        IReadOnlyList<CatalogListingSource> sources,
        CancellationToken cancellationToken)
    {
        var itemsMissingBarcode = sources.Count(source => string.IsNullOrWhiteSpace(source.Barcode));

        var externalCategoryId = await categoryMappings.ResolveExternalIdAsync(
            marketplace,
            categoryId,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(externalCategoryId))
        {
            return new ChannelReadinessDto(
                marketplace.Code,
                marketplace.Name,
                CategoryMapped: false,
                Ready: false,
                sources.Count,
                itemsMissingBarcode,
                []);
        }

        var requiredAttributes = (await externalAttributes.ListByCategoryAsync(
                marketplace,
                externalCategoryId,
                cancellationToken))
            .Where(attribute => attribute.Required)
            .ToList();

        var mappings = await attributeMappings.ListAsync(
            marketplace,
            categoryId,
            sourceType: null,
            AllMappings,
            cancellationToken);

        var mappingsByExternalId = mappings
            .GroupBy(mapping => mapping.ExternalAttributeId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);

        var missing = new List<MissingChannelAttributeDto>();
        foreach (var required in requiredAttributes)
        {
            if (!mappingsByExternalId.TryGetValue(required.ExternalAttributeId, out var attributeMappingList))
            {
                missing.Add(new MissingChannelAttributeDto(
                    required.ExternalAttributeId,
                    required.Name,
                    Reason: "unmapped",
                    sources.Count));
                continue;
            }

            // Aynı dış özelliğe birden çok PIM kaynağı eşlenebilir (özellik + varyant);
            // kalem, eşlemelerden HERHANGİ biri için değer taşıyorsa özellik dolu sayılır.
            var missingItemCount = sources.Count(source => !attributeMappingList.Any(mapping =>
                source.Attributes.Any(selection =>
                    selection.IsVariant == (mapping.SourceType == AttributeMappingSourceType.CatalogVariant)
                    && selection.SourceId == mapping.CatalogSourceId)));

            if (missingItemCount > 0)
            {
                missing.Add(new MissingChannelAttributeDto(
                    required.ExternalAttributeId,
                    required.Name,
                    Reason: "unfilled",
                    missingItemCount));
            }
        }

        return new ChannelReadinessDto(
            marketplace.Code,
            marketplace.Name,
            CategoryMapped: true,
            Ready: missing.Count == 0 && itemsMissingBarcode == 0,
            sources.Count,
            itemsMissingBarcode,
            missing);
    }
}
