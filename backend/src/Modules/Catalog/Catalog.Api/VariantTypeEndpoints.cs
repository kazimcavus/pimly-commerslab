using Catalog.Api.Requests;
using Catalog.Application.Variants.CreateVariantType;
using Catalog.Application.Variants.DeleteVariantType;
using Catalog.Application.Variants.GetVariantType;
using Catalog.Application.Variants.ListVariantTypes;
using Catalog.Application.Variants.UpdateVariantType;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Catalog.Api;

/// <summary>Varyant türü endpoint'lerini tanımlar.</summary>
internal static class VariantTypeEndpoints
{
    internal static void MapVariantTypeEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/variants", async (CreateVariantTypeRequest request, ICreateVariantTypeHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new CreateVariantTypeCommand(
                request.Name,
                request.SelectionStyle ?? "list",
                request.SortOrder,
                request.Slicer));
            return result.ToCreatedResult(dto => $"/api/v1/catalog/variants/{dto.Id}");
        });

        group.MapGet("/variants", async (
            IListVariantTypesHandler handler,
            int page = 0,
            int page_size = 0) =>
        {
            var result = await handler.ExecuteAsync(new ListVariantTypesQuery(page, page_size));
            return result.ToHttpResult();
        });

        group.MapGet("/variants/{id:guid}", async (Guid id, IGetVariantTypeHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new GetVariantTypeQuery(id));
            return result.ToHttpResult();
        });

        group.MapPatch("/variants/{id:guid}", async (
            Guid id,
            UpdateVariantTypeRequest request,
            IUpdateVariantTypeHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new UpdateVariantTypeCommand(
                id,
                request.Name,
                request.SelectionStyle ?? "list",
                request.SortOrder,
                request.Slicer));
            return result.ToHttpResult();
        });

        group.MapDelete("/variants/{id:guid}", async (Guid id, IDeleteVariantTypeHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new DeleteVariantTypeCommand(id));
            return result.ToHttpResult();
        });
    }
}
