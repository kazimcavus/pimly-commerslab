using Catalog.Api.Requests;
using Catalog.Application.CatalogSettings.GetCatalogSettings;
using Catalog.Application.CatalogSettings.UpdateCatalogSettings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Catalog.Api;

/// <summary>Katalog ayarları endpoint'lerini tanımlar.</summary>
internal static class CatalogSettingsEndpoints
{
    internal static void MapCatalogSettingsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/settings", async (IGetCatalogSettingsHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new GetCatalogSettingsQuery());
            return result.ToHttpResult();
        });

        group.MapPut("/settings", async (
            UpdateCatalogSettingsRequest request,
            IUpdateCatalogSettingsHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new UpdateCatalogSettingsCommand(request.SlicerNamePosition));
            return result.ToHttpResult();
        });
    }
}
