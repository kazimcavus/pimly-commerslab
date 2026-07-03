using Catalog.Api.Requests;
using Catalog.Application.Products.DeleteItemChannelPrice;
using Catalog.Application.Products.ListItemChannelPrices;
using Catalog.Application.Products.UpsertItemChannelPrice;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Catalog.Api;

/// <summary>Kalem kanal fiyatı endpoint'lerini tanımlar.</summary>
internal static class ChannelPriceEndpoints
{
    internal static void MapChannelPriceEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/items/{itemId:guid}/channel-prices", async (
            Guid itemId,
            IListItemChannelPricesHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new ListItemChannelPricesQuery(itemId));
            return result.ToHttpResult();
        });

        group.MapPut("/items/{itemId:guid}/channel-prices/{marketplaceKey}", async (
            Guid itemId,
            string marketplaceKey,
            UpsertItemChannelPriceRequest request,
            IUpsertItemChannelPriceHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new UpsertItemChannelPriceCommand(
                itemId,
                marketplaceKey,
                request.Price,
                request.CompareAtPrice,
                request.Currency));
            return result.ToHttpResult();
        });

        group.MapDelete("/items/{itemId:guid}/channel-prices/{marketplaceKey}", async (
            Guid itemId,
            string marketplaceKey,
            IDeleteItemChannelPriceHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new DeleteItemChannelPriceCommand(itemId, marketplaceKey));
            return result.ToHttpResult();
        });
    }
}
