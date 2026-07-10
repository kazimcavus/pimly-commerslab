using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Inventory.Api;

/// <summary>Inventory modülü REST API uç noktalarını kaydeder.</summary>
public static class InventoryEndpoints
{
    /// <summary>Inventory modülü endpoint'lerini uygulama pipeline'ına kaydeder.</summary>
    /// <returns>Kaydedilen route grubu.</returns>
    public static RouteGroupBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/inventory")
            .WithTags("Inventory")
            .RequireAuthorization();

        group.MapStockLevelEndpoints();
        return group;
    }
}
