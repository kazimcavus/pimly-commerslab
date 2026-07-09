using Channels.Application.Contracts;
using Channels.Application.Validation;
using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.ExternalCatalog;
using Channels.Domain.TaxonomySync;
using FluentValidation;
using SharedKernel;

namespace Channels.Application.ExternalCatalog.SearchExternalCategories;

/// <summary>
/// Yerel cache'teki harici pazaryeri kategorilerinde metin tabanlı arama yapar.
/// </summary>
/// <remarks>
/// <para><b>Amaç:</b> Kategori eşleme UI'ında kullanıcının pazaryeri kategorilerini aramasını sağlar;
/// sonuçlar taxonomy sync ile önceden indirilmiş <see cref="IExternalCategoryRepository"/> cache'inden gelir.</para>
/// <para><b>Ön koşullar:</b> Geçerli pazaryeri anahtarı; ilgili pazaryeri için en az bir taxonomy sync
/// tamamlanmış olması önerilir (cache boşsa sonuç dönmeyebilir).</para>
/// <para><b>Ana akış:</b> Sorgu doğrulanır → pazaryeri çözümlenir → cache üzerinde arama yapılır →
/// <see cref="ExternalCategoryDto"/> listesi döndürülür.</para>
/// <para><b>Hata durumları:</b> Doğrulama hatası, geçersiz pazaryeri anahtarı, kayıtlı olmayan pazaryeri.</para>
/// <para><b>API:</b> Herkese açık HTTP API endpoint'i üzerinden kullanılır.</para>
/// </remarks>
public sealed class SearchExternalCategoriesHandler(
    IValidator<SearchExternalCategoriesQuery> validator,
    IExternalCategoryRepository externalCategories) : ISearchExternalCategoriesHandler
{
    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<ExternalCategoryDto>>> ExecuteAsync(
        SearchExternalCategoriesQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(query, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<ExternalCategoryDto>>(validationResult.Error);
        }

        var marketplaceResult = Marketplace.FromCode(query.MarketplaceCode);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<ExternalCategoryDto>>(marketplaceResult.Error);
        }

        var marketplace = marketplaceResult.Value;

        var categories = await externalCategories.SearchAsync(
            marketplace,
            query.Query,
            query.Limit,
            cancellationToken);

        IReadOnlyList<ExternalCategoryDto> dtos = categories
            .Select(category => category.ToDto())
            .ToList();

        return Result.Success(dtos);
    }
}
