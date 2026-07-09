using FluentValidation;
using Pricing.Application.Contracts;
using Pricing.Application.ItemPrices.Catalog;
using Pricing.Application.Validation;
using Pricing.Domain.ItemPrices;
using Pricing.Domain.PriceDefinitions;
using SharedKernel;

namespace Pricing.Application.ItemPrices.ListItemPrices;

/// <summary>Kalemin fiyat tanımı bazlı fiyatlarını listeleme işlemini yürüten handler.</summary>
public sealed class ListItemPricesHandler(
    IValidator<ListItemPricesQuery> validator,
    ICatalogProductItemGateway productItems,
    IItemPriceRepository itemPrices,
    IPriceDefinitionRepository priceDefinitions) : IListItemPricesHandler
{
    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<ItemPriceDto>>> ExecuteAsync(
        ListItemPricesQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(query, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<ItemPriceDto>>(validationResult.Error);
        }

        if (!await productItems.ExistsAsync(query.ProductItemId, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<ItemPriceDto>>(Error.NotFound("Product item not found."));
        }

        // Tanım adı, ürün okuma DTO'larındaki marka adı çözümlemesi gibi sözlükle join edilir.
        var definitionNamesById = (await priceDefinitions.ListAsync(cancellationToken))
            .ToDictionary(definition => definition.Id, definition => definition.Name);

        var prices = await itemPrices.ListByItemAsync(query.ProductItemId, cancellationToken);
        return Result.Success<IReadOnlyList<ItemPriceDto>>(prices
            .Select(price => price.ToDto(
                definitionNamesById.TryGetValue(price.PriceDefinitionId, out var name) ? name : string.Empty))
            .ToList());
    }
}
