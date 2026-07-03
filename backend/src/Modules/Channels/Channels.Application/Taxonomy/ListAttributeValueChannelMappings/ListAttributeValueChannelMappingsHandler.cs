using Channels.Application.Contracts;
using Channels.Application.Ports;
using Channels.Application.Taxonomy.AttributeChannelMappingSupport;
using Channels.Application.Validation;
using Channels.Domain.Marketplaces;
using Channels.Domain.Taxonomy;
using FluentValidation;
using SharedKernel;

namespace Channels.Application.Taxonomy.ListAttributeValueChannelMappings;

/// <summary>
/// Belirli bir attribute/variant kanal eşlemesi altındaki tüm değer eşlemelerini listeler.
/// </summary>
/// <remarks>
/// <para><b>Amaç:</b> Bir alan eşlemesine bağlı Catalog-harici değer eşlemelerini yönetim arayüzünde
/// Catalog ve harici değer detaylarıyla birlikte sunar.</para>
/// <para><b>Ön koşullar:</b> Geçerli pazaryeri, Catalog kategori ve mevcut üst
/// <see cref="AttributeChannelMapping"/> kaydı.</para>
/// <para><b>Ana akış:</b> Sorgu doğrulanır → üst alan eşlemesi getirilir → alt değer eşlemeleri
/// listelenir → <see cref="AttributeChannelMappingSupport.AttributeChannelMappingEnricher"/> ile
/// zenginleştirilir.</para>
/// <para><b>Hata durumları:</b> Doğrulama hatası, üst eşleme bulunamadı veya bağlam uyuşmazlığı (NotFound).</para>
/// <para><b>API:</b> Herkese açık HTTP API endpoint'i üzerinden kullanılır.</para>
/// </remarks>
public sealed class ListAttributeValueChannelMappingsHandler(
    IValidator<ListAttributeValueChannelMappingsQuery> validator,
    ICategoryChannelMappingRepository categoryMappings,
    IAttributeChannelMappingRepository fieldMappings,
    IAttributeValueChannelMappingRepository valueMappings,
    IExternalAttributeValueRepository externalValues,
    ICatalogAttributeGateway catalogAttributes,
    ICatalogVariantGateway catalogVariants) : IListAttributeValueChannelMappingsHandler
{
    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<AttributeValueChannelMappingDto>>> ExecuteAsync(
        ListAttributeValueChannelMappingsQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(query, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<AttributeValueChannelMappingDto>>(validationResult.Error);
        }

        var keyResult = MarketplaceKey.Create(query.MarketplaceKey);
        if (keyResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<AttributeValueChannelMappingDto>>(keyResult.Error);
        }

        var marketplaceResult = MarketplaceRegistry.GetByKey(keyResult.Value);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<AttributeValueChannelMappingDto>>(marketplaceResult.Error);
        }

        var parentMapping = await fieldMappings.GetByIdAsync(query.MappingId, cancellationToken);
        if (parentMapping is null
            || parentMapping.MarketplaceKey != keyResult.Value
            || parentMapping.CatalogCategoryId != query.CatalogCategoryId)
        {
            return Result.Failure<IReadOnlyList<AttributeValueChannelMappingDto>>(
                Error.NotFound("Attribute channel mapping not found."));
        }

        var items = await valueMappings.ListByFieldMappingAsync(parentMapping.Id, cancellationToken);

        var dtos = await AttributeChannelMappingEnricher.EnrichValuesAsync(
            items,
            parentMapping,
            categoryMappings,
            externalValues,
            catalogAttributes,
            catalogVariants,
            cancellationToken);

        return Result.Success(dtos);
    }
}
