using Channels.Application.AttributeChannelMappings.AttributeChannelMappingSupport;
using Channels.Application.AttributeChannelMappings.Catalog;
using Channels.Application.Contracts;
using Channels.Application.Validation;
using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.ExternalCatalog;
using Channels.Domain.Marketplaces;
using Channels.Domain.TaxonomySync;
using FluentValidation;
using SharedKernel;

namespace Channels.Application.AttributeChannelMappings.ListAttributeChannelMappings;

/// <summary>
/// Belirli bir Catalog kategorisi için tanımlı attribute/variant kanal eşlemelerini sayfalı olarak listeler.
/// </summary>
/// <remarks>
/// <para><b>Amaç:</b> Kategori bazında tüm alan eşlemelerini yönetim arayüzünde sayfalama ile sunar;
/// isteğe bağlı kaynak tipi filtresi destekler.</para>
/// <para><b>Ön koşullar:</b> Geçerli pazaryeri anahtarı ve Catalog kategori kimliği.</para>
/// <para><b>Ana akış:</b> Sorgu ve sayfalama doğrulanır → isteğe bağlı kaynak tipi filtresi uygulanır →
/// eşlemeler listelenir → <see cref="AttributeChannelMappingSupport.AttributeChannelMappingEnricher"/> ile
/// zenginleştirilir.</para>
/// <para><b>Hata durumları:</b> Doğrulama hatası, geçersiz sayfalama, geçersiz kaynak tipi veya pazaryeri.</para>
/// <para><b>API:</b> Herkese açık HTTP API endpoint'i üzerinden kullanılır.</para>
/// </remarks>
public sealed class ListAttributeChannelMappingsHandler(
    IValidator<ListAttributeChannelMappingsQuery> validator,
    ICategoryChannelMappingRepository categoryMappings,
    IAttributeChannelMappingRepository mappings,
    IExternalCategoryAttributeRepository externalAttributes,
    ICatalogAttributeGateway catalogAttributes,
    ICatalogVariantGateway catalogVariants) : IListAttributeChannelMappingsHandler
{
    /// <inheritdoc/>
    public async Task<Result<PagedResult<AttributeChannelMappingDto>>> ExecuteAsync(
        ListAttributeChannelMappingsQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(query, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<PagedResult<AttributeChannelMappingDto>>(validationResult.Error);
        }

        var paginationResult = PaginationSupport.Resolve(query.Page, query.PageSize);
        if (paginationResult.IsFailure)
        {
            return Result.Failure<PagedResult<AttributeChannelMappingDto>>(paginationResult.Error);
        }

        var marketplaceResult = Marketplace.FromCode(query.MarketplaceCode);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure<PagedResult<AttributeChannelMappingDto>>(marketplaceResult.Error);
        }

        var marketplace = marketplaceResult.Value;

        AttributeMappingSourceType? sourceTypeFilter = null;
        if (!string.IsNullOrWhiteSpace(query.SourceType))
        {
            var sourceTypeResult = AttributeMappingSourceTypeParser.Parse(query.SourceType);
            if (sourceTypeResult.IsFailure)
            {
                return Result.Failure<PagedResult<AttributeChannelMappingDto>>(sourceTypeResult.Error);
            }

            sourceTypeFilter = sourceTypeResult.Value;
        }

        var pagination = paginationResult.Value;
        var items = await mappings.ListAsync(
            marketplace,
            query.CatalogCategoryId,
            sourceTypeFilter,
            pagination,
            cancellationToken);

        var totalCount = await mappings.CountAsync(
            marketplace,
            query.CatalogCategoryId,
            sourceTypeFilter,
            cancellationToken);

        var enrichedItems = await AttributeChannelMappingEnricher.EnrichManyAsync(
            items,
            categoryMappings,
            externalAttributes,
            catalogAttributes,
            catalogVariants,
            cancellationToken);

        return Result.Success(new PagedResult<AttributeChannelMappingDto>(
            enrichedItems,
            pagination.Page,
            pagination.PageSize,
            totalCount));
    }
}
