using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Pricing.Api;

/// <summary>Pricing modülü REST API uç noktalarını kaydeder.</summary>
public static class PricingEndpoints
{
    /// <summary>Pricing modülü endpoint'lerini uygulama pipeline'ına kaydeder.</summary>
    /// <returns>Kaydedilen route grubu.</returns>
    public static RouteGroupBuilder MapPricingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pricing")
            .WithTags("Pricing")
            .RequireAuthorization();

        group.MapPriceDefinitionEndpoints();
        group.MapItemPriceEndpoints();
        group.MapBasePriceEndpoints();
        group.MapChannelPriceEndpoints();
        return group;
    }
}
