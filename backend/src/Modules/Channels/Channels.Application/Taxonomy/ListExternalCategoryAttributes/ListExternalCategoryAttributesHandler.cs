using Channels.Application.Contracts;
using Channels.Application.Validation;
using Channels.Domain;
using Channels.Domain.Marketplaces;
using Channels.Domain.Taxonomy;
using FluentValidation;
using SharedKernel;

namespace Channels.Application.Taxonomy.ListExternalCategoryAttributes;

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
    IMarketplaceCategoryAttributesClient attributesClient,
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

        var keyResult = MarketplaceKey.Create(query.MarketplaceKey);
        if (keyResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<ExternalCategoryAttributeDto>>(keyResult.Error);
        }

        var marketplaceResult = MarketplaceRegistry.GetByKey(keyResult.Value);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<ExternalCategoryAttributeDto>>(marketplaceResult.Error);
        }

        var externalCategoryId = await categoryMappings.ResolveExternalIdAsync(
            keyResult.Value,
            query.CatalogCategoryId,
            cancellationToken);

        if (externalCategoryId is null)
        {
            return Result.Failure<IReadOnlyList<ExternalCategoryAttributeDto>>(
                Error.NotFound("Category channel mapping required before listing external attributes."));
        }

        var fetchResult = await attributesClient.FetchCategoryAttributesAsync(
            marketplaceResult.Value,
            externalCategoryId,
            cancellationToken);

        if (fetchResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<ExternalCategoryAttributeDto>>(fetchResult.Error);
        }

        var syncedAt = timeProvider.GetUtcNow();
        var upserts = fetchResult.Value
            .Select(attribute => new ExternalCategoryAttributeUpsert(
                attribute.ExternalAttributeId,
                attribute.Name,
                attribute.Required,
                attribute.AllowCustom,
                attribute.IsVariant,
                attribute.Values
                    .Select(value => new ExternalAttributeValueUpsert(value.ExternalValueId, value.Name))
                    .ToList()))
            .ToList();

        await externalAttributes.UpsertBatchAsync(
            keyResult.Value,
            externalCategoryId,
            upserts,
            syncedAt,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var attributes = await externalAttributes.ListByCategoryAsync(
            keyResult.Value,
            externalCategoryId,
            cancellationToken);

        var values = await externalValues.ListByCategoryAsync(
            keyResult.Value,
            externalCategoryId,
            cancellationToken);

        IReadOnlyList<ExternalCategoryAttributeDto> dtos = attributes
            .Select(attribute => attribute.ToDto(values))
            .ToList();

        return Result.Success(dtos);
    }
}
