// Not: Catalog.Application.CatalogSettings ad alanı tip adını gölgelediği için takma ad kullanılır.
using CatalogSettingsEntity = Catalog.Domain.Settings.CatalogSettings;

namespace Catalog.Application.Contracts;

/// <summary>Katalog ayarları DTO'su.</summary>
public sealed record CatalogSettingsDto(string SlicerNamePosition);

/// <summary>CatalogSettings domain modeli ile DTO arasında dönüşüm sağlar.</summary>
internal static class CatalogSettingsMappings
{
    public static CatalogSettingsDto ToDto(this CatalogSettingsEntity settings) =>
        new(settings.SlicerNamePosition);
}
