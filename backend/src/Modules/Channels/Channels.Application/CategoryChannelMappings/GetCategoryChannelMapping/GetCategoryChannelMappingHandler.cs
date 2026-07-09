using Channels.Application.CategoryChannelMappings.Catalog;
using Channels.Application.CategoryChannelMappings.CategoryChannelMappingSupport;
using Channels.Application.Contracts;
using Channels.Application.Validation;
using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.ExternalCatalog;
using Channels.Domain.TaxonomySync;
using FluentValidation;
using SharedKernel;

namespace Channels.Application.CategoryChannelMappings.GetCategoryChannelMapping;

/// <summary>
/// Belirli bir Catalog kategorisi için tanımlı tek kategori kanal eşlemesini getirir.
/// </summary>
/// <remarks>
/// <para><b>Amaç:</b> Kategori eşleme detayını Catalog ve harici kategori bilgileriyle zenginleştirilmiş
/// olarak sunar.</para>
/// <para><b>Ön koşullar:</b> Geçerli pazaryeri anahtarı ve daha önce oluşturulmuş bir
/// <see cref="CategoryChannelMapping"/> kaydı.</para>
/// <para><b>Ana akış:</b> Sorgu doğrulanır → eşleme depodan getirilir →
/// <see cref="CategoryChannelMappingSupport.CategoryChannelMappingEnricher"/> ile Catalog ve harici kategori
/// detayları eklenir.</para>
/// <para><b>Hata durumları:</b> Doğrulama hatası, geçersiz pazaryeri, eşleme bulunamadı (NotFound).</para>
/// <para><b>API:</b> Herkese açık HTTP API endpoint'i üzerinden kullanılır.</para>
/// </remarks>
public sealed class GetCategoryChannelMappingHandler(
    IValidator<GetCategoryChannelMappingQuery> validator,
    ICategoryChannelMappingRepository mappings,
    IExternalCategoryRepository externalCategories,
    ICatalogCategoryGateway catalogCategories) : IGetCategoryChannelMappingHandler
{
    /// <inheritdoc/>
    public async Task<Result<CategoryChannelMappingDto>> ExecuteAsync(
        GetCategoryChannelMappingQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(query, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<CategoryChannelMappingDto>(validationResult.Error);
        }

        var marketplaceResult = Marketplace.FromCode(query.MarketplaceCode);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure<CategoryChannelMappingDto>(marketplaceResult.Error);
        }

        var marketplace = marketplaceResult.Value;

        var mapping = await mappings.GetAsync(marketplace, query.CatalogCategoryId, cancellationToken);
        if (mapping is null)
        {
            return Result.Failure<CategoryChannelMappingDto>(Error.NotFound("Category channel mapping not found."));
        }

        var dto = await CategoryChannelMappingEnricher.EnrichAsync(
            mapping,
            externalCategories,
            catalogCategories,
            cancellationToken);

        return Result.Success(dto);
    }
}
