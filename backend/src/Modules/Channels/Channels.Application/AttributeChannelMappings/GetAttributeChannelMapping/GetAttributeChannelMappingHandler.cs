using Channels.Application.AttributeChannelMappings.AttributeChannelMappingSupport;
using Channels.Application.AttributeChannelMappings.Catalog;
using Channels.Application.Contracts;
using Channels.Application.Validation;
using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.ExternalCatalog;
using Channels.Domain.TaxonomySync;
using FluentValidation;
using SharedKernel;

namespace Channels.Application.AttributeChannelMappings.GetAttributeChannelMapping;

/// <summary>
/// Belirli bir attribute/variant kanal eşlemesini kimliği ile getirir.
/// </summary>
/// <remarks>
/// <para><b>Amaç:</b> Tek bir alan eşlemesinin Catalog kaynağı, harici attribute ve kategori bağlamıyla
/// zenginleştirilmiş detayını sunar.</para>
/// <para><b>Ön koşullar:</b> Geçerli pazaryeri, Catalog kategori ve mevcut
/// <see cref="AttributeChannelMapping"/> kaydı.</para>
/// <para><b>Ana akış:</b> Sorgu doğrulanır → eşleme kimliği ile getirilir → pazaryeri/kategori
/// eşleşmesi kontrol edilir → <see cref="AttributeChannelMappingSupport.AttributeChannelMappingEnricher"/>
/// ile zenginleştirilir.</para>
/// <para><b>Hata durumları:</b> Doğrulama hatası, eşleme bulunamadı veya bağlam uyuşmazlığı (NotFound).</para>
/// <para><b>API:</b> Herkese açık HTTP API endpoint'i üzerinden kullanılır.</para>
/// </remarks>
public sealed class GetAttributeChannelMappingHandler(
    IValidator<GetAttributeChannelMappingQuery> validator,
    ICategoryChannelMappingRepository categoryMappings,
    IAttributeChannelMappingRepository mappings,
    IExternalCategoryAttributeRepository externalAttributes,
    ICatalogAttributeGateway catalogAttributes,
    ICatalogVariantGateway catalogVariants) : IGetAttributeChannelMappingHandler
{
    /// <inheritdoc/>
    public async Task<Result<AttributeChannelMappingDto>> ExecuteAsync(
        GetAttributeChannelMappingQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(query, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<AttributeChannelMappingDto>(validationResult.Error);
        }

        var marketplaceResult = Marketplace.FromCode(query.MarketplaceCode);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure<AttributeChannelMappingDto>(marketplaceResult.Error);
        }

        var marketplace = marketplaceResult.Value;

        var mapping = await mappings.GetByIdAsync(query.MappingId, cancellationToken);
        if (mapping is null
            || mapping.Marketplace != marketplace
            || mapping.CatalogCategoryId != query.CatalogCategoryId)
        {
            return Result.Failure<AttributeChannelMappingDto>(Error.NotFound("Attribute channel mapping not found."));
        }

        var dto = await AttributeChannelMappingEnricher.EnrichAsync(
            mapping,
            categoryMappings,
            externalAttributes,
            catalogAttributes,
            catalogVariants,
            cancellationToken);

        return Result.Success(dto);
    }
}
