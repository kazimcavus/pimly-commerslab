using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.CatalogSettings.GetCatalogSettings;

/// <summary>Katalog ayarlarını getirme handler sözleşmesi.</summary>
public interface IGetCatalogSettingsHandler
{
    Task<Result<CatalogSettingsDto>> ExecuteAsync(
        GetCatalogSettingsQuery query,
        CancellationToken cancellationToken = default);
}
