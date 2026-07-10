using Inventory.Api.Requests;
using Inventory.Application.StockLevels.GetStock;
using Inventory.Application.StockLevels.SetStock;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Pimly.AspNetCore;

namespace Inventory.Api;

/// <summary>Kalem stok seviyesi endpoint'lerini tanımlar.</summary>
internal static class StockLevelEndpoints
{
    internal static void MapStockLevelEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/items/{itemId:guid}/stock", async (
            Guid itemId,
            IGetStockHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new GetStockQuery(itemId));
            return result.ToHttpResult();
        });

        group.MapPut("/items/{itemId:guid}/stock", async (
            Guid itemId,
            SetStockRequest request,
            ISetStockHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new SetStockCommand(itemId, request.Quantity));
            return result.ToHttpResult();
        });
    }
}
