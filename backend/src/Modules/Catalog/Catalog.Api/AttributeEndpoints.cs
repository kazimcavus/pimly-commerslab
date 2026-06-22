using Catalog.Api.Requests;
using Catalog.Application.Attributes.CreateAttribute;
using Catalog.Application.Attributes.DeleteAttribute;
using Catalog.Application.Attributes.GetAttribute;
using Catalog.Application.Attributes.ListAttributes;
using Catalog.Application.Attributes.UpdateAttribute;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Pimly.AspNetCore;

namespace Catalog.Api;

/// <summary>Öznitelik tanımı endpoint'lerini tanımlar.</summary>
internal static class AttributeEndpoints
{
    internal static void MapAttributeEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/attributes", async (CreateAttributeRequest request, ICreateAttributeHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new CreateAttributeCommand(request.Name));
            return result.ToCreatedResult(dto => $"/api/v1/catalog/attributes/{dto.Id}");
        });

        group.MapGet("/attributes", async (
            IListAttributesHandler handler,
            int page = 0,
            int page_size = 0) =>
        {
            var result = await handler.ExecuteAsync(new ListAttributesQuery(page, page_size));
            return result.ToHttpResult();
        });

        group.MapGet("/attributes/{id:guid}", async (Guid id, IGetAttributeHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new GetAttributeQuery(id));
            return result.ToHttpResult();
        });

        group.MapPatch("/attributes/{id:guid}", async (Guid id, UpdateAttributeRequest request, IUpdateAttributeHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new UpdateAttributeCommand(id, request.Name));
            return result.ToHttpResult();
        });

        group.MapDelete("/attributes/{id:guid}", async (Guid id, IDeleteAttributeHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new DeleteAttributeCommand(id));
            return result.ToHttpResult();
        });
    }
}
