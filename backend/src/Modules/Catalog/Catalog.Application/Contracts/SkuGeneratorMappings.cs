using Catalog.Application.Contracts;
using Catalog.Domain.SkuGenerator;

namespace Catalog.Application.Contracts;

/// <summary>SkuGenerator domain → DTO dönüşümleri.</summary>
public static class SkuGeneratorMappings
{
    public static SkuGeneratorConfigDto ToDto(this SkuGeneratorConfig config) =>
        new(
            config.Enabled,
            config.Segments.Select(ToDto).ToList(),
            config.CounterNextValue);

    public static SkuSegmentDto ToDto(this SkuSegment segment) =>
        new(
            segment.Type,
            segment.Label,
            segment.Value,
            segment.Start,
            segment.Width,
            segment.Digits,
            segment.Source);

    public static SkuSegment ToDomain(this SkuSegmentDto segment) =>
        new()
        {
            Type = segment.Type,
            Label = segment.Label,
            Value = segment.Value,
            Start = segment.Start,
            Width = segment.Width,
            Digits = segment.Digits,
            Source = segment.Source,
        };
}
