using Channels.Application.Contracts;
using Channels.Application.Validation;
using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.ExternalCatalog;
using Channels.Domain.TaxonomySync;
using FluentValidation;
using SharedKernel;

namespace Channels.Application.AttributeChannelMappings.ResolveAttributeValueChannelMapping;

/// <summary>
/// Catalog attribute veya variant değeri için tanımlı kanal eşlemesinden harici pazaryeri değer
/// kimliğini çözümler.
/// </summary>
/// <remarks>
/// <para><b>Amaç:</b> Ürün yayınlama (publish) pipeline'ında Catalog değer kimliğini pazaryerinin
/// beklediği harici value id'sine dönüştürür.</para>
/// <para><b>Ön koşullar:</b> Geçerli üst <see cref="AttributeChannelMapping"/> ve ilgili Catalog değeri
/// için tanımlı bir <see cref="AttributeValueChannelMapping"/> kaydı.</para>
/// <para><b>Ana akış:</b> Sorgu doğrulanır → depodan harici value id çözümlenir →
/// <see cref="ResolvedAttributeValueChannelMappingDto"/> döndürülür.</para>
/// <para><b>Hata durumları:</b> Doğrulama hatası, değer eşlemesi bulunamadı (NotFound).</para>
/// <para><b>API:</b> Yalnızca dahili kullanım; ürün yayınlama pipeline'ı tarafından çağrılır, HTTP API'de
/// doğrudan endpoint yoktur.</para>
/// </remarks>
public sealed class ResolveAttributeValueChannelMappingHandler(
    IValidator<ResolveAttributeValueChannelMappingQuery> validator,
    IAttributeValueChannelMappingRepository valueMappings) : IResolveAttributeValueChannelMappingHandler
{
    /// <inheritdoc/>
    public async Task<Result<ResolvedAttributeValueChannelMappingDto>> ExecuteAsync(
        ResolveAttributeValueChannelMappingQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(query, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<ResolvedAttributeValueChannelMappingDto>(validationResult.Error);
        }

        var externalValueId = await valueMappings.ResolveExternalValueIdAsync(
            query.AttributeChannelMappingId,
            query.CatalogValueId,
            cancellationToken);

        if (externalValueId is null)
        {
            return Result.Failure<ResolvedAttributeValueChannelMappingDto>(
                Error.NotFound("Attribute value channel mapping not found."));
        }

        return Result.Success(new ResolvedAttributeValueChannelMappingDto(
            query.AttributeChannelMappingId,
            query.CatalogValueId,
            externalValueId));
    }
}
