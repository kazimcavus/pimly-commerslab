using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Pimly.AspNetCore;
using Pricing.Api.Requests;
using Pricing.Application.ChannelPrices.GetChannelPrice;
using Pricing.Application.ChannelPrices.ListChannelPrices;
using Pricing.Application.ChannelPrices.SetChannelPrice;

namespace Pricing.Api;

/// <summary>Kalem kanal (pazaryeri) fiyatı endpoint'lerini tanımlar.</summary>
internal static class ChannelPriceEndpoints
{
    internal static void MapChannelPriceEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/items/{itemId:guid}/channel-prices", async (
            Guid itemId,
            IListChannelPricesHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new ListChannelPricesQuery(itemId));
            return result.ToHttpResult();
        });

        group.MapGet("/items/{itemId:guid}/channel-prices/{marketplace}", async (
            Guid itemId,
            string marketplace,
            IGetChannelPriceHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new GetChannelPriceQuery(itemId, marketplace));
            return result.ToHttpResult();
        });

        group.MapPut("/items/{itemId:guid}/channel-prices/{marketplace}", async (
            Guid itemId,
            string marketplace,
            SetChannelPriceRequest request,
            ISetChannelPriceHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new SetChannelPriceCommand(
                itemId,
                marketplace,
                request.Amount,
                request.CompareAtAmount,
                request.Currency));
            return result.ToHttpResult();
        });
    }
}
