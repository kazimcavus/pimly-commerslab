using Channels.Application.Contracts;
using Channels.Application.Ports;
using Channels.Application.Taxonomy.AttributeChannelMappingSupport;
using Channels.Application.Validation;
using Channels.Domain.Marketplaces;
using Channels.Domain.Taxonomy;
using FluentValidation;
using SharedKernel;

namespace Channels.Application.Taxonomy.GetAttributeChannelMapping;

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

        var keyResult = MarketplaceKey.Create(query.MarketplaceKey);
        if (keyResult.IsFailure)
        {
            return Result.Failure<AttributeChannelMappingDto>(keyResult.Error);
        }

        var marketplaceResult = MarketplaceRegistry.GetByKey(keyResult.Value);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure<AttributeChannelMappingDto>(marketplaceResult.Error);
        }

        var mapping = await mappings.GetByIdAsync(query.MappingId, cancellationToken);
        if (mapping is null
            || mapping.MarketplaceKey != keyResult.Value
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
