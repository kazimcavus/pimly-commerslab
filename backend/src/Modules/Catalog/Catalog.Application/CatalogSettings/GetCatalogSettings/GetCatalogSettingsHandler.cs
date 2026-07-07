using Catalog.Application.Contracts;
using Catalog.Domain;
using Catalog.Domain.Settings;
using SharedKernel;

namespace Catalog.Application.CatalogSettings.GetCatalogSettings;

/// <summary>Katalog ayarlarını getirme işlemini yürütür; yoksa varsayılanları oluşturur.</summary>
public sealed class GetCatalogSettingsHandler(
    ICatalogSettingsRepository settingsRepository,
    IUnitOfWork unitOfWork) : IGetCatalogSettingsHandler
{
    /// <inheritdoc/>
    public async Task<Result<CatalogSettingsDto>> ExecuteAsync(
        GetCatalogSettingsQuery query,
        CancellationToken cancellationToken = default)
    {
        _ = query;

        var settings = await settingsRepository.GetAsync(cancellationToken);
        if (settings is null)
        {
            settings = Domain.Settings.CatalogSettings.CreateInitial();
            await settingsRepository.AddAsync(settings, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(settings.ToDto());
    }
}
