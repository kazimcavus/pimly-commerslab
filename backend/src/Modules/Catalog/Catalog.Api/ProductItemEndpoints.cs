using Catalog.Api.Requests;
using Catalog.Application.Products.AddProductItem;
using Catalog.Application.Products.CreateProduct;
using Catalog.Application.Products.DeleteProductItem;
using Catalog.Application.Products.GetProductItem;
using Catalog.Application.Products.UpdateProductItem;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Catalog.Api;

/// <summary>Ürün kalemi (ProductItem) endpoint'lerini tanımlar.</summary>
internal static class ProductItemEndpoints
{
    internal static void MapProductItemEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/items/{id:guid}", async (Guid id, IGetProductItemHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new GetProductItemQuery(id));
            return result.ToHttpResult();
        });

        group.MapPost("/products/{productId:guid}/items", async (
            Guid productId,
            CreateProductItemRequest request,
            IAddProductItemHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new AddProductItemCommand(
                productId,
                new CreateProductItemInput(
                    request.Sku,
                    request.Barcode,
                    request.Gtin,
                    request.Mpn,
                    request.AxisValueEntryId,
                    request.AxisValue,
                    request.Stock,
                    ProductInputMapper.MapAttributeValues(request.AttributeValues),
                    ProductInputMapper.MapVariantValues(request.VariantValues))));
            return result.ToHttpResult();
        });

        group.MapPatch("/items/{id:guid}", async (Guid id, UpdateProductItemRequest request, IUpdateProductItemHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new UpdateProductItemCommand(
                id,
                request.Gtin,
                request.Mpn,
                request.AxisValueEntryId,
                request.AxisValue,
                request.Stock,
                ProductInputMapper.MapAttributeValues(request.AttributeValues),
                request.Sku,
                request.Barcode));
            return result.ToHttpResult();
        });

        group.MapDelete("/items/{id:guid}", async (Guid id, IDeleteProductItemHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new DeleteProductItemCommand(id));
            return result.ToHttpResult();
        });
    }
}
