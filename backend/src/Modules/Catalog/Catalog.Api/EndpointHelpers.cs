using System.Text.Json;

namespace Catalog.Api;

/// <summary>Endpoint tanımlarında kullanılan ortak yardımcı metodlar.</summary>
internal static class EndpointHelpers
{
    internal static JsonDocument? ParseJson(JsonElement? element)
    {
        if (element is null || element.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return JsonDocument.Parse(element.Value.GetRawText());
    }
}
