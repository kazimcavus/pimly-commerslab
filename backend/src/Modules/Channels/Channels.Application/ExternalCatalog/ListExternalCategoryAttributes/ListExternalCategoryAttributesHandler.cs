using Channels.Application.Contracts;
using Channels.Application.Validation;
using Channels.Domain;
using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.ExternalCatalog;
using Channels.Domain.Marketplaces;
using Channels.Domain.TaxonomySync;
using FluentValidation;
using SharedKernel;

namespace Channels.Application.ExternalCatalog.ListExternalCategoryAttributes;

/// <summary>
/// Eşlenmiş bir Catalog kategorisi için pazaryerindeki harici kategori attribute'larını canlı olarak
/// çeker, yerel cache'e yazar ve listeler.
/// </summary>
/// <remarks>
/// <para><b>Amaç:</b> Attribute eşleme UI'ına pazaryerinin o kategori için beklediği alanları (zorunluluk,
/// varyant, değer listesi vb.) sunar; her çağrıda pazaryeri API'sinden güncel veri alınır.</para>
/// <para><b>Ön koşullar:</b> İlgili Catalog kategorisi için tanımlı bir
/// <see cref="CategoryChannelMapping"/> kaydı (kategori eşlemesi zorunludur).</para>
/// <para><b>Ana akış:</b> Sorgu doğrulanır → kategori eşlemesinden harici kategori id çözümlenir →
/// <see cref="IMarketplaceCategoryAttributesClient"/> ile attribute'lar çekilir → cache'e toplu upsert
/// edilir → değerlerle birlikte <see cref="ExternalCategoryAttributeDto"/> listesi döndürülür.</para>
/// <para><b>Hata durumları:</b> Kategori eşlemesi yok (NotFound), pazaryeri API hatası, doğrulama hatası.</para>
/// <para><b>API:</b> Herkese açık HTTP API endpoint'i üzerinden kullanılır.</para>
/// </remarks>
public sealed class ListExternalCategoryAttributesHandler(
    IValidator<ListExternalCategoryAttributesQuery> validator,
    ICategoryChannelMappingRepository categoryMappings,
    IExternalCategoryAttributeRepository externalAttributes,
    IExternalAttributeValueRepository externalValues,
    IMarketplaceCategoryAttributesClientResolver attributesClientResolver,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IListExternalCategoryAttributesHandler
{
    public async Task<Result<IReadOnlyList<ExternalCategoryAttributeDto>>> ExecuteAsync(
        ListExternalCategoryAttributesQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(query, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<ExternalCategoryAttributeDto>>(validationResult.Error);
        }

        var marketplaceResult = Marketplace.FromCode(query.MarketplaceCode);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<ExternalCategoryAttributeDto>>(marketplaceResult.Error);
        }

        var marketplace = marketplaceResult.Value;

        var clientResult = attributesClientResolver.Resolve(marketplace);
        if (clientResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<ExternalCategoryAttributeDto>>(clientResult.Error);
        }

        var externalCategoryId = await categoryMappings.ResolveExternalIdAsync(
            marketplace,
            query.CatalogCategoryId,
            cancellationToken);

        if (externalCategoryId is null)
        {
            return Result.Failure<IReadOnlyList<ExternalCategoryAttributeDto>>(
                Error.NotFound("Category channel mapping required before listing external attributes."));
        }

        // Ortak destek IsVariant/IsSlicer dahil cache'i tazeler; ürün import hattı da aynı yolu kullanır.
        var refreshResult = await ExternalCategoryAttributeCacheSupport.RefreshAsync(
            clientResult.Value,
            externalAttributes,
            marketplace,
            externalCategoryId,
            timeProvider.GetUtcNow(),
            cancellationToken);

        if (refreshResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<ExternalCategoryAttributeDto>>(refreshResult.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var attributes = await externalAttributes.ListByCategoryAsync(
            marketplace,
            externalCategoryId,
            cancellationToken);

        var values = await externalValues.ListByCategoryAsync(
            marketplace,
            externalCategoryId,
            cancellationToken);

        IReadOnlyList<ExternalCategoryAttributeDto> dtos = attributes
            .Select(attribute => attribute.ToDto(values))
            .ToList();

        return Result.Success(dtos);
    }
}
