using System.Text.Json.Serialization;

namespace Catalog.Api.Requests;

/// <summary>Katalog ayarları güncelleme isteği.</summary>
internal sealed record UpdateCatalogSettingsRequest(
    [property: JsonPropertyName("slicer_name_position")] string SlicerNamePosition);
