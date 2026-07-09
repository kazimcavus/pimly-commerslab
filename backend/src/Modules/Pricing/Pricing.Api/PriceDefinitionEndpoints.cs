using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Pimly.AspNetCore;
using Pricing.Api.Requests;
using Pricing.Application.PriceDefinitions.CreatePriceDefinition;
using Pricing.Application.PriceDefinitions.DeletePriceDefinition;
using Pricing.Application.PriceDefinitions.GetPriceDefinition;
using Pricing.Application.PriceDefinitions.ListPriceDefinitions;
using Pricing.Application.PriceDefinitions.UpdatePriceDefinition;

namespace Pricing.Api;

/// <summary>Fiyat tanımı endpoint'lerini tanımlar.</summary>
internal static class PriceDefinitionEndpoints
{
    internal static void MapPriceDefinitionEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/price-definitions", async (
            CreatePriceDefinitionRequest request,
            ICreatePriceDefinitionHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new CreatePriceDefinitionCommand(request.Name, request.Code));
            return result.ToCreatedResult(dto => $"/api/v1/pricing/price-definitions/{dto.Id}");
        });

        group.MapGet("/price-definitions", async (
            IListPriceDefinitionsHandler handler,
            int page = 0,
            int page_size = 0) =>
        {
            var result = await handler.ExecuteAsync(new ListPriceDefinitionsQuery(page, page_size));
            return result.ToHttpResult();
        });

        group.MapGet("/price-definitions/{id:guid}", async (Guid id, IGetPriceDefinitionHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new GetPriceDefinitionQuery(id));
            return result.ToHttpResult();
        });

        group.MapPatch("/price-definitions/{id:guid}", async (
            Guid id,
            UpdatePriceDefinitionRequest request,
            IUpdatePriceDefinitionHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new UpdatePriceDefinitionCommand(id, request.Name, request.Code));
            return result.ToHttpResult();
        });

        group.MapDelete("/price-definitions/{id:guid}", async (Guid id, IDeletePriceDefinitionHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new DeletePriceDefinitionCommand(id));
            return result.ToHttpResult();
        });
    }
}
