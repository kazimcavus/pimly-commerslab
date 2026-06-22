using Catalog.Api.Requests;
using Catalog.Application.Variants.AddVariantValue;
using Catalog.Application.Variants.ListVariantValues;
using Catalog.Application.Variants.RemoveVariantValue;
using Catalog.Application.Variants.UpdateVariantValue;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Catalog.Api;

/// <summary>Varyant değeri endpoint'lerini tanımlar.</summary>
internal static class VariantValueEndpoints
{
    internal static void MapVariantValueEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/variants/{id:guid}/values", async (
            Guid id,
            VariantValueRequest request,
            IAddVariantValueHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new AddVariantValueCommand(
                id,
                request.Label,
                request.Color,
                request.ImageUrl,
                request.Code,
                request.SortOrder));
            return result.ToCreatedResult(dto => $"/api/v1/catalog/variant-values/{dto.Id}");
        });

        group.MapGet("/variants/{id:guid}/values", async (
            Guid id,
            IListVariantValuesHandler handler,
            int page = 0,
            int page_size = 0) =>
        {
            var result = await handler.ExecuteAsync(new ListVariantValuesQuery(id, page, page_size));
            return result.ToHttpResult();
        });

        group.MapPatch("/variant-values/{id:guid}", async (
            Guid id,
            VariantValueRequest request,
            IUpdateVariantValueHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new UpdateVariantValueCommand(
                id,
                request.Label,
                request.Color,
                request.ImageUrl,
                request.Code,
                request.SortOrder));
            return result.ToHttpResult();
        });

        group.MapDelete("/variant-values/{id:guid}", async (Guid id, IRemoveVariantValueHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new RemoveVariantValueCommand(id));
            return result.ToHttpResult();
        });
    }
}
