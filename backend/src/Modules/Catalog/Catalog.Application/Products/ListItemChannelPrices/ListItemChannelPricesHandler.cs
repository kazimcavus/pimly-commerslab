using Catalog.Application.Contracts;
using Catalog.Application.Validation;
using Catalog.Domain;
using Catalog.Domain.Products;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Products.ListItemChannelPrices;

/// <summary>Kalemin kanal fiyatlarını listeleme işlemini yürüten handler.</summary>
public sealed class ListItemChannelPricesHandler(
    IValidator<ListItemChannelPricesQuery> validator,
    IProductRepository products,
    IChannelPriceRepository channelPrices) : IListItemChannelPricesHandler
{
    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<ChannelPriceDto>>> ExecuteAsync(
        ListItemChannelPricesQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(query, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<ChannelPriceDto>>(validationResult.Error);
        }

        var item = await products.GetItemByIdAsync(query.ProductItemId, cancellationToken);
        if (item is null)
        {
            return Result.Failure<IReadOnlyList<ChannelPriceDto>>(Error.NotFound("Product item not found."));
        }

        var prices = await channelPrices.ListByItemAsync(query.ProductItemId, cancellationToken);
        return Result.Success<IReadOnlyList<ChannelPriceDto>>(
            prices.Select(price => price.ToDto()).ToList());
    }
}
