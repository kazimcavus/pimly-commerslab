namespace Catalog.Application.CatalogSettings.UpdateCatalogSettings;

/// <summary>Katalog ayarlarını güncelleme komutu.</summary>
/// <param name="SlicerNamePosition">Ayraç değeri ad konumu; "suffix" veya "prefix".</param>
public sealed record UpdateCatalogSettingsCommand(string SlicerNamePosition);
