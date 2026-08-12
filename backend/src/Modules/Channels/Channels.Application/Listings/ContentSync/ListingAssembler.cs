using Channels.Application.Listings.OfferSync;
using Channels.Application.Publications;
using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using SharedKernel;

namespace Channels.Application.Listings.ContentSync;

/// <summary>
/// Kanonik katalog içeriğini pazaryeri-nötr listeleme payload'ına çevirir. Kategori ve özellik
/// eşlemeleri burada uygulanır; pazaryerine özgü tel formatı ise istemcinin işidir.
/// </summary>
/// <remarks>
/// <para><b>Ön koşul:</b> Ürünün kategorisi pazaryeri kategorisine eşlenmiş olmalı — eşleme yoksa
/// kalem gönderilemez ve gerekçesiyle atlanır.</para>
/// <para><b>Eşlenmemiş değerler:</b> Özellik eşlemesi varsa ama değer eşlemesi yoksa değer serbest
/// metin olarak gönderilir; pazaryeri kabul etmezse hata kalem düzeyinde raporlanır. Özellik
/// eşlemesi hiç yoksa o özellik payload'dan düşülür.</para>
/// </remarks>
public sealed class ListingAssembler(
    ICategoryChannelMappingRepository categoryMappings,
    IAttributeChannelMappingRepository attributeMappings,
    IAttributeValueChannelMappingRepository valueMappings)
{
    /// <summary>Bir kalemin içeriğini fiyat ve stokla birleştirip payload üretir.</summary>
    /// <param name="marketplace">Hedef pazaryeri.</param>
    /// <param name="source">Catalog'dan okunan içerik.</param>
    /// <param name="price">Pricing'de kararlaştırılmış kanal fiyatı.</param>
    /// <param name="quantity">Inventory'deki güncel stok.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Gönderilebilir payload veya atlanma gerekçesi.</returns>
    public async Task<Result<MarketplaceListingRequest>> AssembleAsync(
        Marketplace marketplace,
        CatalogListingSource source,
        DecidedChannelPrice price,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        var externalCategoryId = await categoryMappings.ResolveExternalIdAsync(
            marketplace,
            source.CategoryId,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(externalCategoryId))
        {
            return Result.Failure<MarketplaceListingRequest>(
                Error.Validation("Ürünün kategorisi pazaryeri kategorisine eşlenmemiş."));
        }

        if (string.IsNullOrWhiteSpace(source.Barcode))
        {
            return Result.Failure<MarketplaceListingRequest>(
                Error.Validation("Kalemin barkodu yok; pazaryerinde kimliklendirilemez."));
        }

        var attributes = new List<MarketplaceListingAttribute>();
        foreach (var selection in source.Attributes)
        {
            var mapped = await MapSelectionAsync(marketplace, source.CategoryId, selection, cancellationToken);
            if (mapped is not null)
            {
                attributes.Add(mapped);
            }
        }

        return Result.Success(new MarketplaceListingRequest(
            source.ProductItemId,
            source.Barcode,
            source.Title,
            source.Description,
            externalCategoryId,
            source.BrandExternalCode,
            source.BrandName,
            source.ModelCode,
            source.Sku,
            price.Amount,
            price.CompareAtAmount,
            price.Currency,
            quantity,
            attributes,
            source.ImageUrls));
    }

    private async Task<MarketplaceListingAttribute?> MapSelectionAsync(
        Marketplace marketplace,
        Guid categoryId,
        CatalogListingSelection selection,
        CancellationToken cancellationToken)
    {
        var sourceType = selection.IsVariant
            ? AttributeMappingSourceType.CatalogVariant
            : AttributeMappingSourceType.CatalogAttribute;

        var mapping = await attributeMappings.GetAsync(
            marketplace,
            categoryId,
            sourceType,
            selection.SourceId,
            cancellationToken);

        if (mapping is null)
        {
            // Pazaryerinde karşılığı olmayan özellik gönderilmez.
            return null;
        }

        var externalValueId = await valueMappings.ResolveExternalValueIdAsync(
            mapping.Id,
            selection.ValueId,
            cancellationToken);

        return externalValueId is null
            ? new MarketplaceListingAttribute(mapping.ExternalAttributeId, null, selection.ValueLabel)
            : new MarketplaceListingAttribute(mapping.ExternalAttributeId, externalValueId, null);
    }
}
