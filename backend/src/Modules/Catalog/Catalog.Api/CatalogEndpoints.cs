using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Catalog.Api;

/// <summary>Catalog modülü REST API uç noktalarını kaydeder.</summary>
public static class CatalogEndpoints
{
    /// <summary>Catalog modülü endpoint'lerini uygulama pipeline'ına kaydeder.</summary>
    /// <returns>Kaydedilen route grubu.</returns>
    public static RouteGroupBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/catalog")
            .WithTags("Catalog")
            .RequireAuthorization();

        group.MapCategoryEndpoints();
        group.MapAttributeEndpoints();
        group.MapAttributeValueEndpoints();
        group.MapVariantTypeEndpoints();
        group.MapVariantValueEndpoints();
        group.MapProductEndpoints();
        group.MapProductItemEndpoints();
        group.MapBarcodeEndpoints();

        return group;
    }
}
