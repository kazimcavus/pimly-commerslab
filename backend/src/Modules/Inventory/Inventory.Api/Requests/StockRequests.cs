using System.Text.Json.Serialization;

namespace Inventory.Api.Requests;

/// <summary>Kalem stok miktarı oluşturma / güncelleme isteği.</summary>
public sealed record SetStockRequest(
    [property: JsonPropertyName("quantity")] int Quantity);
