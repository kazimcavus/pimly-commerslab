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

namespace Channels.Application.CategoryChannelMappings.ListCategoryChannelMappings;

/// <summary>
/// Pazaryeri için tanımlı kategori kanal eşlemelerini sayfalı olarak listeler.
/// </summary>
/// <remarks>
/// <para><b>Amaç:</b> Yönetim arayüzünde tüm veya belirli bir Catalog kategorisine ait eşlemeleri
/// sayfalama ile sunar.</para>
/// <para><b>Ön koşullar:</b> Geçerli pazaryeri anahtarı; isteğe bağlı Catalog kategori filtresi.</para>
/// <para><b>Ana akış:</b> Sorgu ve sayfalama doğrulanır → eşlemeler listelenir → toplam sayı alınır →
/// <see cref="CategoryChannelMappingSupport.CategoryChannelMappingEnricher"/> ile zenginleştirilir.</para>
/// <para><b>Hata durumları:</b> Doğrulama hatası, geçersiz sayfalama, geçersiz pazaryeri.</para>
/// <para><b>API:</b> Herkese açık HTTP API endpoint'i üzerinden kullanılır.</para>
/// </remarks>
public sealed class ListCategoryChannelMappingsHandler(
    IValidator<ListCategoryChannelMappingsQuery> validator,
    ICategoryChannelMappingRepository mappings,
    IExternalCategoryRepository externalCategories,
    ICatalogCategoryGateway catalogCategories) : IListCategoryChannelMappingsHandler
{
    /// <inheritdoc/>
    public async Task<Result<PagedResult<CategoryChannelMappingDto>>> ExecuteAsync(
        ListCategoryChannelMappingsQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(query, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<PagedResult<CategoryChannelMappingDto>>(validationResult.Error);
        }

        var paginationResult = PaginationSupport.Resolve(query.Page, query.PageSize);
        if (paginationResult.IsFailure)
        {
            return Result.Failure<PagedResult<CategoryChannelMappingDto>>(paginationResult.Error);
        }

        var marketplaceResult = Marketplace.FromCode(query.MarketplaceCode);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure<PagedResult<CategoryChannelMappingDto>>(marketplaceResult.Error);
        }

        var marketplace = marketplaceResult.Value;

        var pagination = paginationResult.Value;
        var items = await mappings.ListAsync(
            marketplace,
            query.CatalogCategoryId,
            pagination,
            cancellationToken);

        var totalCount = await mappings.CountAsync(
            marketplace,
            query.CatalogCategoryId,
            cancellationToken);

        var enrichedItems = await CategoryChannelMappingEnricher.EnrichManyAsync(
            items,
            externalCategories,
            catalogCategories,
            cancellationToken);

        return Result.Success(new PagedResult<CategoryChannelMappingDto>(
            enrichedItems,
            pagination.Page,
            pagination.PageSize,
            totalCount));
    }
}
