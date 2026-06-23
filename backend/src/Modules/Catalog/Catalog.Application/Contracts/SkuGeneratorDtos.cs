namespace Catalog.Application.Contracts;

/// <summary>SKU segment DTO'su.</summary>
public sealed record SkuSegmentDto(
    string Type,
    string? Label,
    string? Value,
    int? Start,
    int? Width,
    int? Digits,
    string? Source);

/// <summary>SKU oluşturucu yapılandırma DTO'su.</summary>
public sealed record SkuGeneratorConfigDto(
    bool Enabled,
    IReadOnlyList<SkuSegmentDto> Segments,
    long CounterNextValue);
