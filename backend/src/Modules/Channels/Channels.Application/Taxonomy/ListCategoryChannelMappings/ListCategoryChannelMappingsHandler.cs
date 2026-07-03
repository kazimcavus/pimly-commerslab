using Channels.Application.Contracts;
using Channels.Application.Ports;
using Channels.Application.Taxonomy.CategoryChannelMappingSupport;
using Channels.Application.Validation;
using Channels.Domain.Marketplaces;
using Channels.Domain.Taxonomy;
using FluentValidation;
using SharedKernel;

namespace Channels.Application.Taxonomy.ListCategoryChannelMappings;

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

        var keyResult = MarketplaceKey.Create(query.MarketplaceKey);
        if (keyResult.IsFailure)
        {
            return Result.Failure<PagedResult<CategoryChannelMappingDto>>(keyResult.Error);
        }

        var marketplaceResult = MarketplaceRegistry.GetByKey(keyResult.Value);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure<PagedResult<CategoryChannelMappingDto>>(marketplaceResult.Error);
        }

        var pagination = paginationResult.Value;
        var items = await mappings.ListAsync(
            keyResult.Value,
            query.CatalogCategoryId,
            pagination,
            cancellationToken);

        var totalCount = await mappings.CountAsync(
            keyResult.Value,
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
