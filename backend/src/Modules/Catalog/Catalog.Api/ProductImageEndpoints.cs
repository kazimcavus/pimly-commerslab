using Catalog.Api.Requests;
using Catalog.Application.Products.AddProductImage;
using Catalog.Application.Products.RemoveProductImage;
using Catalog.Application.Products.UpdateProductImage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Catalog.Api;

/// <summary>Ürün galerisi görsel endpoint'lerini tanımlar.</summary>
internal static class ProductImageEndpoints
{
    internal static void MapProductImageEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/products/{id:guid}/images", async (
            Guid id,
            AddProductImageRequest request,
            IAddProductImageHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new AddProductImageCommand(
                id,
                request.Url,
                request.SortOrder,
                request.AltText,
                request.IsPrimary,
                request.VariantValueId));

            return result.ToCreatedResult(dto => $"/api/v1/catalog/product-images/{dto.Id}");
        });

        group.MapPatch("/product-images/{id:guid}", async (
            Guid id,
            UpdateProductImageRequest request,
            IUpdateProductImageHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new UpdateProductImageCommand(
                id,
                request.Url,
                request.SortOrder,
                request.AltText,
                request.IsPrimary,
                request.VariantValueId));

            return result.ToHttpResult();
        });

        group.MapDelete("/product-images/{id:guid}", async (
            Guid id,
            IRemoveProductImageHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new RemoveProductImageCommand(id));
            return result.ToHttpResult();
        });
    }
}
