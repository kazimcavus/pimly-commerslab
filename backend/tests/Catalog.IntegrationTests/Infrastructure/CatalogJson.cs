using System.Text.Json;

namespace Catalog.IntegrationTests.Infrastructure;

/// <summary>API'nin snake_case tel formatıyla uyumlu test serileştirme ayarları.</summary>
internal static class CatalogJson
{
    /// <summary>Yanıt gövdelerini snake_case anahtarlardan PascalCase DTO'lara bağlayan ayarlar.</summary>
    internal static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower,
    };
}
