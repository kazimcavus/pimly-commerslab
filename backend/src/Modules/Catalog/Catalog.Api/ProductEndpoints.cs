using Catalog.Api.Requests;
using Catalog.Application.Products.CreateProduct;
using Catalog.Application.Products.CreateProductsBatch;
using Catalog.Application.Products.DeleteProduct;
using Catalog.Application.Products.GetProduct;
using Catalog.Application.Products.ListProducts;
using Catalog.Application.Products.UpdateProduct;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Catalog.Api;

/// <summary>Ürün endpoint'lerini tanımlar.</summary>
internal static class ProductEndpoints
{
    internal static void MapProductEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/products", async (CreateProductRequest request, ICreateProductHandler handler) =>
        {
            var result = await handler.ExecuteAsync(MapCreateProductCommand(request));
            return result.ToCreatedResult(dto => $"/api/v1/catalog/products/{dto.Id}");
        });

        group.MapPost("/products:batch", async (CreateProductsBatchRequest request, ICreateProductsBatchHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new CreateProductsBatchCommand(
                request.GroupId,
                MapBatchItems(request.Products)));

            return result.ToCreatedResult(r => $"/api/v1/catalog/products/{r.Products[0].Id}");
        });

        group.MapGet("/products", async (
            IListProductsHandler handler,
            int page = 0,
            int page_size = 0) =>
        {
            var result = await handler.ExecuteAsync(new ListProductsQuery(page, page_size));
            return result.ToHttpResult();
        });

        group.MapGet("/products/{id:guid}", async (Guid id, IGetProductHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new GetProductQuery(id));
            return result.ToHttpResult();
        });

        group.MapPatch("/products/{id:guid}", async (Guid id, UpdateProductRequest request, IUpdateProductHandler handler) =>
        {
            var attributeValues = request.AttributeValues is null
                ? null
                : ProductInputMapper.MapAttributeValues(request.AttributeValues);
            var result = await handler.ExecuteAsync(new UpdateProductCommand(
                id,
                request.Name,
                request.Status,
                attributeValues));
            return result.ToHttpResult();
        });

        group.MapDelete("/products/{id:guid}", async (Guid id, IDeleteProductHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new DeleteProductCommand(id));
            return result.ToHttpResult();
        });
    }

    private static CreateProductCommand MapCreateProductCommand(CreateProductRequest request) =>
        new(
            request.GroupId,
            request.ModelCode,
            request.Name,
            request.Status,
            ProductInputMapper.MapAttributeValues(request.AttributeValues),
            ProductInputMapper.MapVariants(request.Variants),
            MapItemInputs(request.Items));

    private static CreateProductsBatchItem MapBatchItem(BatchProductRequest request) =>
        new(
            request.ModelCode,
            request.Name,
            request.Status,
            ProductInputMapper.MapAttributeValues(request.AttributeValues),
            ProductInputMapper.MapVariants(request.Variants),
            MapItemInputs(request.Items));

    private static List<CreateProductItemInput> MapItemInputs(
        IReadOnlyList<CreateProductItemRequest>? items) =>
        (items ?? []).Select(item => new CreateProductItemInput(
            item.Sku,
            item.Barcode,
            item.Gtin,
            item.Mpn,
            item.AxisValueEntryId,
            item.AxisValue,
            item.Price,
            item.CompareAtPrice,
            item.Stock,
            ProductInputMapper.MapAttributeValues(item.AttributeValues),
            ProductInputMapper.MapVariantValues(item.VariantValues))).ToList();

    private static List<CreateProductsBatchItem> MapBatchItems(
        IReadOnlyList<BatchProductRequest>? products) =>
        (products ?? []).Select(MapBatchItem).ToList();
}
