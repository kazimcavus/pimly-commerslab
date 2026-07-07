using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.CatalogSettings.UpdateCatalogSettings;

/// <summary>Katalog ayarlarını güncelleme handler sözleşmesi.</summary>
public interface IUpdateCatalogSettingsHandler
{
    Task<Result<CatalogSettingsDto>> ExecuteAsync(
        UpdateCatalogSettingsCommand command,
        CancellationToken cancellationToken = default);
}
