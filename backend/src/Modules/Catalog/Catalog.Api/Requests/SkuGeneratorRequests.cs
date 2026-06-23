using System.Text.Json.Serialization;
using Catalog.Application.Contracts;

namespace Catalog.Api.Requests;

/// <summary>SKU oluşturucu yapılandırmasını güncelleme isteği.</summary>
public sealed record UpdateSkuGeneratorConfigRequest(
    bool Enabled,
    IReadOnlyList<SkuSegmentDto> Segments,
    [property: JsonPropertyName("counter_next_value")] long? CounterNextValue);
