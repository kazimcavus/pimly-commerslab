using Catalog.Application.Contracts;
using Catalog.Domain;
using SharedKernel;

namespace Catalog.Application.Products.GetProductItem;

/// <summary>Ürün varyantı getirme işlemini yürüten handler.</summary>
public sealed class GetProductItemHandler(IProductRepository products) : IGetProductItemHandler
{
    /// <inheritdoc/>
    public async Task<Result<ProductItemDto>> ExecuteAsync(
        GetProductItemQuery query,
        CancellationToken cancellationToken = default)
    {
        var product = await products.GetByItemIdAsync(query.Id, cancellationToken);
        if (product is null)
        {
            return Result.Failure<ProductItemDto>(Error.NotFound("Product variant not found."));
        }

        var variant = product.Items.FirstOrDefault(v => v.Id == query.Id);
        return variant is null
            ? Result.Failure<ProductItemDto>(Error.NotFound("Product variant not found."))
            : Result.Success(variant.ToDto(product.Id));
    }
}
