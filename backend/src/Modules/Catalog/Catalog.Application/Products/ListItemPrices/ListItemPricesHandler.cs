using Catalog.Application.Contracts;
using Catalog.Application.Validation;
using Catalog.Domain;
using Catalog.Domain.Products;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Products.ListItemPrices;

/// <summary>Kalemin fiyat tanımı bazlı fiyatlarını listeleme işlemini yürüten handler.</summary>
public sealed class ListItemPricesHandler(
    IValidator<ListItemPricesQuery> validator,
    IProductRepository products,
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

        var item = await products.GetItemByIdAsync(query.ProductItemId, cancellationToken);
        if (item is null)
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
