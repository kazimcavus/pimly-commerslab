using Catalog.Api.Requests;
using Catalog.Application.Attributes.AddAttributeValue;
using Catalog.Application.Attributes.ListAttributeValues;
using Catalog.Application.Attributes.RemoveAttributeValue;
using Catalog.Application.Attributes.UpdateAttributeValue;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Catalog.Api;

/// <summary>Özellik değeri endpoint'lerini tanımlar.</summary>
internal static class AttributeValueEndpoints
{
    internal static void MapAttributeValueEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/attributes/{id:guid}/values", async (
            Guid id,
            AttributeValueRequest request,
            IAddAttributeValueHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new AddAttributeValueCommand(id, request.Name));
            return result.ToCreatedResult(dto => $"/api/v1/catalog/attribute-values/{dto.Id}");
        });

        group.MapGet("/attributes/{id:guid}/values", async (
            Guid id,
            IListAttributeValuesHandler handler,
            int page = 0,
            int page_size = 0) =>
        {
            var result = await handler.ExecuteAsync(new ListAttributeValuesQuery(id, page, page_size));
            return result.ToHttpResult();
        });

        group.MapPatch("/attribute-values/{id:guid}", async (
            Guid id,
            AttributeValueRequest request,
            IUpdateAttributeValueHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new UpdateAttributeValueCommand(id, request.Name));
            return result.ToHttpResult();
        });

        group.MapDelete("/attribute-values/{id:guid}", async (Guid id, IRemoveAttributeValueHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new RemoveAttributeValueCommand(id));
            return result.ToHttpResult();
        });
    }
}
