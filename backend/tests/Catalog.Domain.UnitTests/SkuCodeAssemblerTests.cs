using Catalog.Domain.SkuGenerator;
using Catalog.Domain.Variants;
using FluentAssertions;

namespace Catalog.Domain.UnitTests;

/// <summary>SkuCodeAssembler için birim testleri.</summary>
public class SkuCodeAssemblerTests
{
    private static readonly DateTime FixedDate = new(2025, 6, 23, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void AssembleProductCode_MatchesDocumentExample()
    {
        var segments = new List<SkuSegment>
        {
            new() { Type = SkuSegmentTypes.Fixed, Value = "26" },
            new() { Type = SkuSegmentTypes.Year, Digits = 2 },
            new() { Type = SkuSegmentTypes.Counter, Start = 1000, Width = 4 },
            new() { Type = SkuSegmentTypes.Color, Source = "code" },
            new() { Type = SkuSegmentTypes.Size, Source = "code" },
        };

        var result = SkuCodeAssembler.AssembleProductCode(segments, null, 1000, FixedDate);

        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("26251000");
        result.Value.NextCounter.Should().Be(1001);
    }

    [Fact]
    public void AssembleVariantSku_MatchesDocumentExample()
    {
        var segments = new List<SkuSegment>
        {
            new() { Type = SkuSegmentTypes.Fixed, Value = "26" },
            new() { Type = SkuSegmentTypes.Year, Digits = 2 },
            new() { Type = SkuSegmentTypes.Counter, Start = 1000, Width = 4 },
            new() { Type = SkuSegmentTypes.Color, Source = "code" },
            new() { Type = SkuSegmentTypes.Size, Source = "code" },
        };

        var selections = new[]
        {
            new SkuVariantSelection(SelectionStyle.Color, "Kırmızı", "R08"),
            new SkuVariantSelection(SelectionStyle.List, "M", "M"),
        };

        var sku = SkuCodeAssembler.AssembleVariantSku("26251000", segments, selections);

        sku.Should().Be("26251000R08M");
    }

    [Fact]
    public void VariantToken_UsesNameWhenSourceIsName()
    {
        var selection = new SkuVariantSelection(SelectionStyle.Color, "Mavi", "B01");

        SkuCodeAssembler.VariantToken(selection, "name").Should().Be("MAVI");
    }

    [Fact]
    public void VariantToken_FallsBackToNameWhenKeyMissing()
    {
        var selection = new SkuVariantSelection(SelectionStyle.List, "Large", null);

        SkuCodeAssembler.VariantToken(selection, "key").Should().Be("LARGE");
    }

    [Fact]
    public void AssembleProductCode_CounterZeroPadsWithDefaultWidth()
    {
        var segments = new List<SkuSegment>
        {
            new() { Type = SkuSegmentTypes.Counter, Start = 1, Width = 4 },
        };

        var result = SkuCodeAssembler.AssembleProductCode(segments, null, 7, FixedDate);

        result.Value.Code.Should().Be("0007");
        result.Value.NextCounter.Should().Be(8);
    }

    [Fact]
    public void AssembleProductCode_YearFourDigits()
    {
        var segments = new List<SkuSegment>
        {
            new() { Type = SkuSegmentTypes.Year, Digits = 4 },
        };

        var result = SkuCodeAssembler.AssembleProductCode(segments, null, 1, FixedDate);

        result.Value.Code.Should().Be("2025");
    }

    [Fact]
    public void ValidateManualInputs_FailsWhenMissing()
    {
        var segments = new List<SkuSegment>
        {
            new() { Type = SkuSegmentTypes.Manual, Label = "Sezon" },
        };

        var result = SkuCodeAssembler.ValidateManualInputs(segments, [string.Empty]);

        result.IsFailure.Should().BeTrue();
    }
}
