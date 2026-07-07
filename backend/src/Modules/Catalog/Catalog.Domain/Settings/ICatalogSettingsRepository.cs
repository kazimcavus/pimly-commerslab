namespace Catalog.Domain.Settings;

/// <summary>Katalog ayarları kalıcılık sözleşmesi.</summary>
public interface ICatalogSettingsRepository
{
    /// <summary>Tenant katalog ayarlarını getirir.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<CatalogSettings?> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>İlk katalog ayarlarını kalıcı depoya ekler.</summary>
    /// <param name="settings">Eklenecek ayarlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task AddAsync(CatalogSettings settings, CancellationToken cancellationToken = default);

    /// <summary>Katalog ayarlarındaki değişiklikleri izlemeye alır.</summary>
    /// <param name="settings">Güncellenmiş ayarlar.</param>
    void Update(CatalogSettings settings);
}
