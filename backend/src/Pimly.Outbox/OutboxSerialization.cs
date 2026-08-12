using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pimly.Outbox;

/// <summary>Outbox payload'ı için yazma ve okuma tarafında ortak JSON ayarları (drift önlenir).</summary>
public static class OutboxSerialization
{
    /// <summary>Integration olaylarının serialize/deserialize ayarları.</summary>
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };
}
