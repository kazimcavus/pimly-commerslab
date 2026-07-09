using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Pimly.AspNetCore;
using Pricing.Api.Requests;
using Pricing.Application.ItemPrices.DeleteItemPrice;
using Pricing.Application.ItemPrices.ListItemPrices;
using Pricing.Application.ItemPrices.UpsertItemPrice;

namespace Pricing.Api;

/// <summary>Kalem fiyatı (fiyat tanımı bazlı tutar) endpoint'lerini tanımlar.</summary>
internal static class ItemPriceEndpoints
{
    internal static void MapItemPriceEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/items/{itemId:guid}/prices", async (
            Guid itemId,
            IListItemPricesHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new ListItemPricesQuery(itemId));
            return result.ToHttpResult();
        });

        group.MapPut("/items/{itemId:guid}/prices/{definitionId:guid}", async (
            Guid itemId,
            Guid definitionId,
            UpsertItemPriceRequest request,
            IUpsertItemPriceHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new UpsertItemPriceCommand(
                itemId,
                definitionId,
                request.Amount,
                request.Currency));
            return result.ToHttpResult();
        });

        group.MapDelete("/items/{itemId:guid}/prices/{definitionId:guid}", async (
            Guid itemId,
            Guid definitionId,
            IDeleteItemPriceHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new DeleteItemPriceCommand(itemId, definitionId));
            return result.ToHttpResult();
        });
    }
}
