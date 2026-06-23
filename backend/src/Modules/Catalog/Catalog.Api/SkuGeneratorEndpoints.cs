using Catalog.Api.Requests;
using Catalog.Application.SkuGenerator.GetSkuGeneratorConfig;
using Catalog.Application.SkuGenerator.UpdateSkuGeneratorConfig;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Catalog.Api;

/// <summary>SKU oluşturucu endpoint'lerini tanımlar.</summary>
internal static class SkuGeneratorEndpoints
{
    internal static void MapSkuGeneratorEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/sku-config", async (IGetSkuGeneratorConfigHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new GetSkuGeneratorConfigQuery());
            return result.ToHttpResult();
        });

        group.MapPut("/sku-config", async (
            UpdateSkuGeneratorConfigRequest request,
            IUpdateSkuGeneratorConfigHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new UpdateSkuGeneratorConfigCommand(
                request.Enabled,
                request.Segments,
                request.CounterNextValue));

            return result.ToHttpResult();
        });
    }
}
