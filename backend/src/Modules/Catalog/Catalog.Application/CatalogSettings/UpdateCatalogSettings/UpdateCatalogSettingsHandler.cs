using Catalog.Application.Contracts;
using Catalog.Domain;
using Catalog.Domain.Settings;
using SharedKernel;

namespace Catalog.Application.CatalogSettings.UpdateCatalogSettings;

/// <summary>Katalog ayarlarını güncelleme işlemini yürütür; ayarlar yoksa önce oluşturur.</summary>
public sealed class UpdateCatalogSettingsHandler(
    ICatalogSettingsRepository settingsRepository,
    IUnitOfWork unitOfWork) : IUpdateCatalogSettingsHandler
{
    /// <inheritdoc/>
    public async Task<Result<CatalogSettingsDto>> ExecuteAsync(
        UpdateCatalogSettingsCommand command,
        CancellationToken cancellationToken = default)
    {
        var settings = await settingsRepository.GetAsync(cancellationToken);
        if (settings is null)
        {
            settings = Domain.Settings.CatalogSettings.CreateInitial();
            await settingsRepository.AddAsync(settings, cancellationToken);
        }

        var updateResult = settings.Update(command.SlicerNamePosition?.Trim().ToLowerInvariant() ?? string.Empty);
        if (updateResult.IsFailure)
        {
            return Result.Failure<CatalogSettingsDto>(updateResult.Error);
        }

        settingsRepository.Update(settings);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(settings.ToDto());
    }
}
