using Catalog.Api.Requests;
using Catalog.Application.Brands.CreateBrand;
using Catalog.Application.Brands.DeleteBrand;
using Catalog.Application.Brands.GetBrand;
using Catalog.Application.Brands.ListBrands;
using Catalog.Application.Brands.UpdateBrand;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Catalog.Api;

/// <summary>Marka endpoint'lerini tanımlar.</summary>
internal static class BrandEndpoints
{
    internal static void MapBrandEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/brands", async (CreateBrandRequest request, ICreateBrandHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new CreateBrandCommand(request.Name, request.Code));
            return result.ToCreatedResult(dto => $"/api/v1/catalog/brands/{dto.Id}");
        });

        group.MapGet("/brands", async (
            IListBrandsHandler handler,
            int page = 0,
            int page_size = 0) =>
        {
            var result = await handler.ExecuteAsync(new ListBrandsQuery(page, page_size));
            return result.ToHttpResult();
        });

        group.MapGet("/brands/{id:guid}", async (Guid id, IGetBrandHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new GetBrandQuery(id));
            return result.ToHttpResult();
        });

        group.MapPatch("/brands/{id:guid}", async (Guid id, UpdateBrandRequest request, IUpdateBrandHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new UpdateBrandCommand(id, request.Name, request.Code));
            return result.ToHttpResult();
        });

        group.MapDelete("/brands/{id:guid}", async (Guid id, IDeleteBrandHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new DeleteBrandCommand(id));
            return result.ToHttpResult();
        });
    }
}
