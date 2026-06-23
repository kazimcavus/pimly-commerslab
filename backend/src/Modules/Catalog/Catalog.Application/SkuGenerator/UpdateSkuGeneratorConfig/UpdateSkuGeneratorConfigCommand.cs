using Catalog.Application.Contracts;

namespace Catalog.Application.SkuGenerator.UpdateSkuGeneratorConfig;

/// <summary>SKU oluşturucu yapılandırmasını güncelleme komutu.</summary>
public sealed record UpdateSkuGeneratorConfigCommand(
    bool Enabled,
    IReadOnlyList<SkuSegmentDto> Segments,
    long? CounterNextValue);
