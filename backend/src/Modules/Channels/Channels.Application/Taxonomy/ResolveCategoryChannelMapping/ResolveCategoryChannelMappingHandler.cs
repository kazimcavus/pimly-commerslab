using Channels.Application.Contracts;
using Channels.Application.Validation;
using Channels.Domain.Marketplaces;
using Channels.Domain.Taxonomy;
using FluentValidation;
using SharedKernel;

namespace Channels.Application.Taxonomy.ResolveCategoryChannelMapping;

/// <summary>
/// Catalog kategorisi için tanımlı kategori kanal eşlemesinden harici pazaryeri kategori kimliğini
/// çözümler.
/// </summary>
/// <remarks>
/// <para><b>Amaç:</b> Ürün yayınlama (publish) pipeline'ında Catalog kategori kimliğini pazaryerinin
/// beklediği harici kategori id'sine dönüştürür.</para>
/// <para><b>Ön koşullar:</b> İlgili pazaryeri ve Catalog kategorisi için tanımlı bir
/// <see cref="CategoryChannelMapping"/> kaydı.</para>
/// <para><b>Ana akış:</b> Sorgu doğrulanır → pazaryeri çözümlenir → depodan harici id çözümlenir →
/// <see cref="ResolvedCategoryChannelMappingDto"/> döndürülür.</para>
/// <para><b>Hata durumları:</b> Doğrulama hatası, geçersiz pazaryeri, eşleme bulunamadı (NotFound).</para>
/// <para><b>API:</b> Yalnızca dahili kullanım; ürün yayınlama pipeline'ı tarafından çağrılır, HTTP API'de
/// doğrudan endpoint yoktur.</para>
/// </remarks>
public sealed class ResolveCategoryChannelMappingHandler(
    IValidator<ResolveCategoryChannelMappingQuery> validator,
    ICategoryChannelMappingRepository mappings) : IResolveCategoryChannelMappingHandler
{
    /// <inheritdoc/>
    public async Task<Result<ResolvedCategoryChannelMappingDto>> ExecuteAsync(
        ResolveCategoryChannelMappingQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(query, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<ResolvedCategoryChannelMappingDto>(validationResult.Error);
        }

        var keyResult = MarketplaceKey.Create(query.MarketplaceKey);
        if (keyResult.IsFailure)
        {
            return Result.Failure<ResolvedCategoryChannelMappingDto>(keyResult.Error);
        }

        var marketplaceResult = MarketplaceRegistry.GetByKey(keyResult.Value);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure<ResolvedCategoryChannelMappingDto>(marketplaceResult.Error);
        }

        var externalId = await mappings.ResolveExternalIdAsync(
            keyResult.Value,
            query.CatalogCategoryId,
            cancellationToken);

        if (externalId is null)
        {
            return Result.Failure<ResolvedCategoryChannelMappingDto>(
                Error.NotFound("Category channel mapping not found."));
        }

        return Result.Success(new ResolvedCategoryChannelMappingDto(
            keyResult.Value.Value,
            query.CatalogCategoryId,
            externalId));
    }
}
