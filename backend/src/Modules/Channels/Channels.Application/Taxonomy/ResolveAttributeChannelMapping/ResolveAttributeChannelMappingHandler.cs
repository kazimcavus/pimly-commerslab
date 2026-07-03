using Channels.Application.Contracts;
using Channels.Application.Validation;
using Channels.Domain.Marketplaces;
using Channels.Domain.Taxonomy;
using FluentValidation;
using SharedKernel;

namespace Channels.Application.Taxonomy.ResolveAttributeChannelMapping;

/// <summary>
/// Catalog attribute veya variant kaynağı için tanımlı kanal eşlemesinden harici pazaryeri attribute
/// kimliğini çözümler.
/// </summary>
/// <remarks>
/// <para><b>Amaç:</b> Ürün yayınlama (publish) pipeline'ında Catalog attribute/variant kimliğini
/// pazaryerinin beklediği harici attribute id'sine dönüştürür.</para>
/// <para><b>Ön koşullar:</b> İlgili pazaryeri, Catalog kategorisi, kaynak tipi ve Catalog kaynağı için
/// tanımlı bir <see cref="AttributeChannelMapping"/> kaydı; dolaylı olarak
/// <see cref="CategoryChannelMapping"/> de gereklidir.</para>
/// <para><b>Ana akış:</b> Sorgu doğrulanır → kaynak tipi ayrıştırılır → depodan harici attribute id
/// çözümlenir → <see cref="ResolvedAttributeChannelMappingDto"/> döndürülür.</para>
/// <para><b>Hata durumları:</b> Doğrulama hatası, geçersiz kaynak tipi, eşleme bulunamadı (NotFound).</para>
/// <para><b>API:</b> Yalnızca dahili kullanım; ürün yayınlama pipeline'ı tarafından çağrılır, HTTP API'de
/// doğrudan endpoint yoktur.</para>
/// </remarks>
public sealed class ResolveAttributeChannelMappingHandler(
    IValidator<ResolveAttributeChannelMappingQuery> validator,
    IAttributeChannelMappingRepository mappings) : IResolveAttributeChannelMappingHandler
{
    /// <inheritdoc/>
    public async Task<Result<ResolvedAttributeChannelMappingDto>> ExecuteAsync(
        ResolveAttributeChannelMappingQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(query, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<ResolvedAttributeChannelMappingDto>(validationResult.Error);
        }

        var keyResult = MarketplaceKey.Create(query.MarketplaceKey);
        if (keyResult.IsFailure)
        {
            return Result.Failure<ResolvedAttributeChannelMappingDto>(keyResult.Error);
        }

        var marketplaceResult = MarketplaceRegistry.GetByKey(keyResult.Value);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure<ResolvedAttributeChannelMappingDto>(marketplaceResult.Error);
        }

        var sourceTypeResult = AttributeMappingSourceTypeParser.Parse(query.SourceType);
        if (sourceTypeResult.IsFailure)
        {
            return Result.Failure<ResolvedAttributeChannelMappingDto>(sourceTypeResult.Error);
        }

        var externalAttributeId = await mappings.ResolveExternalAttributeIdAsync(
            keyResult.Value,
            query.CatalogCategoryId,
            sourceTypeResult.Value,
            query.CatalogSourceId,
            cancellationToken);

        if (externalAttributeId is null)
        {
            return Result.Failure<ResolvedAttributeChannelMappingDto>(
                Error.NotFound("Attribute channel mapping not found."));
        }

        return Result.Success(new ResolvedAttributeChannelMappingDto(
            keyResult.Value.Value,
            query.CatalogCategoryId,
            AttributeMappingSourceTypeParser.ToApiValue(sourceTypeResult.Value),
            query.CatalogSourceId,
            externalAttributeId));
    }
}
