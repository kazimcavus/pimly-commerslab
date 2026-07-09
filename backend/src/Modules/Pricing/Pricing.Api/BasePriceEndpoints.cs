using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Pimly.AspNetCore;
using Pricing.Api.Requests;
using Pricing.Application.BasePrices.GetBasePrice;
using Pricing.Application.BasePrices.SetBasePrice;

namespace Pricing.Api;

/// <summary>Kalem temel (site/genel) fiyatı endpoint'lerini tanımlar.</summary>
internal static class BasePriceEndpoints
{
    internal static void MapBasePriceEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/items/{itemId:guid}/base-price", async (
            Guid itemId,
            IGetBasePriceHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new GetBasePriceQuery(itemId));
            return result.ToHttpResult();
        });

        group.MapPut("/items/{itemId:guid}/base-price", async (
            Guid itemId,
            SetBasePriceRequest request,
            ISetBasePriceHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new SetBasePriceCommand(
                itemId,
                request.Amount,
                request.CompareAtAmount,
                request.Currency));
            return result.ToHttpResult();
        });
    }
}
