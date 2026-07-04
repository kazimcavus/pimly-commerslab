using Catalog.Domain.Products;
using Catalog.Domain.Variants;
using FluentAssertions;
using ProductVariantType = Catalog.Domain.Products.Variant;
using ProductVariantValue = Catalog.Domain.Products.VariantValue;

namespace Catalog.Domain.UnitTests;

/// <summary>ProductCreateSplitter için birim testleri.</summary>
public class ProductCreateSplitterTests
{
    private static readonly Guid ColorTypeId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid SizeTypeId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid RedValueId = Guid.Parse("00000000-0000-0000-0000-000000000011");
    private static readonly Guid BlueValueId = Guid.Parse("00000000-0000-0000-0000-000000000012");
    private static readonly Guid SmallValueId = Guid.Parse("00000000-0000-0000-0000-000000000021");
    private static readonly Guid MediumValueId = Guid.Parse("00000000-0000-0000-0000-000000000022");

    [Fact]
    public void Split_WithoutSlicer_ReturnsSinglePlan()
    {
        var variants = new[] { BasicVariant("BC-001") };
        var types = new[] { new ProductVariantType(SizeTypeId, "Size", SelectionStyle.List) };

        var result = ProductCreateSplitter.Split("SKU-001", "Shirt", types, variants);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].ModelCode.Should().Be("SKU-001");
        result.Value[0].Items.Should().HaveCount(1);
    }

    [Fact]
    public void Split_WithSlicer_CreatesSeparateProductsPerSlicerValue()
    {
        var types = new[]
        {
            new ProductVariantType(ColorTypeId, "Color", SelectionStyle.Color, Slicer: true),
            new ProductVariantType(SizeTypeId, "Size", SelectionStyle.List),
        };

        var variants = new[]
        {
            Variant("BC-RED-S", RedValueId, "Red", SmallValueId, "S"),
            Variant("BC-RED-M", RedValueId, "Red", MediumValueId, "M"),
            Variant("BC-BLUE-S", BlueValueId, "Blue", SmallValueId, "S"),
            Variant("BC-BLUE-M", BlueValueId, "Blue", MediumValueId, "M"),
        };

        var result = ProductCreateSplitter.Split("SKU-001", "Shirt", types, variants);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(plan => plan.ModelCode.StartsWith("SKU-001-"));
        result.Value.Should().OnlyContain(plan => plan.Name.StartsWith("Shirt - "));
        result.Value.Should().OnlyContain(plan => plan.Variants.Single().Name == "Size");
        result.Value.SelectMany(plan => plan.Items).Should().HaveCount(4);
        result.Value.SelectMany(plan => plan.Items)
            .Should()
            .OnlyContain(variant => variant.VariantValues!.All(
                selection => selection.Variant.Id != ColorTypeId));
    }

    [Fact]
    public void Split_WithSlicerOnly_KeepsSlicerAxisOnProductAndItems()
    {
        var types = new[]
        {
            new ProductVariantType(ColorTypeId, "Color", SelectionStyle.Color, Slicer: true),
        };

        ProductItemDraft[] items =
        [
            ColorOnlyItem("BC-RED", RedValueId, "Red"),
            ColorOnlyItem("BC-BLUE", BlueValueId, "Blue"),
        ];

        var result = ProductCreateSplitter.Split("SKU-001", "Shirt", types, items);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(plan => plan.Variants.Single().Id == ColorTypeId);
        result.Value.SelectMany(plan => plan.Items).Should().OnlyContain(item =>
            item.VariantValues!.Single().Variant.Id == ColorTypeId);
    }

    [Fact]
    public void Split_WithSlicer_SetsGroupCodeAndSlicerValue()
    {
        var types = new[]
        {
            new ProductVariantType(ColorTypeId, "Color", SelectionStyle.Color, Slicer: true),
            new ProductVariantType(SizeTypeId, "Size", SelectionStyle.List),
        };

        var variants = new[]
        {
            Variant("BC-RED-S", RedValueId, "Red", SmallValueId, "S"),
            Variant("BC-BLUE-S", BlueValueId, "Blue", SmallValueId, "S"),
        };

        var result = ProductCreateSplitter.Split("SKU-001", "Shirt", types, variants);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().OnlyContain(plan => plan.GroupCode == "SKU-001");
        result.Value.Select(plan => plan.SlicerValue).Should().BeEquivalentTo("Red", "Blue");
    }

    [Fact]
    public void Split_WithOverrides_UsesMarketplaceCodeAndTitle()
    {
        var types = new[]
        {
            new ProductVariantType(ColorTypeId, "Color", SelectionStyle.Color, Slicer: true),
            new ProductVariantType(SizeTypeId, "Size", SelectionStyle.List),
        };

        var variants = new[]
        {
            Variant("BC-RED-S", RedValueId, "Red", SmallValueId, "S"),
            Variant("BC-BLUE-S", BlueValueId, "Blue", SmallValueId, "S"),
        };

        ProductSplitOverride[] overrides =
        [
            new("Red", "25CSM02817GR52", "Antrasit Klasik Halı"),
            new("Blue", null, null),
        ];

        var result = ProductCreateSplitter.Split("SKU-001", "Shirt", types, variants, overrides);

        result.IsSuccess.Should().BeTrue();
        var red = result.Value.Single(plan => plan.SlicerValue == "Red");
        red.ModelCode.Should().Be("25CSM02817GR52");
        red.Name.Should().Be("Antrasit Klasik Halı");
        red.GroupCode.Should().Be("SKU-001");

        var blue = result.Value.Single(plan => plan.SlicerValue == "Blue");
        blue.ModelCode.Should().Be("SKU-001-blue");
        blue.Name.Should().Be("Shirt - Blue");
    }

    [Fact]
    public void Split_WithDuplicateOverrideCode_FallsBackToSlugSuffix()
    {
        var types = new[]
        {
            new ProductVariantType(ColorTypeId, "Color", SelectionStyle.Color, Slicer: true),
            new ProductVariantType(SizeTypeId, "Size", SelectionStyle.List),
        };

        var variants = new[]
        {
            Variant("BC-RED-S", RedValueId, "Red", SmallValueId, "S"),
            Variant("BC-BLUE-S", BlueValueId, "Blue", SmallValueId, "S"),
        };

        ProductSplitOverride[] overrides =
        [
            new("Red", "SAME-CODE", null),
            new("Blue", "SAME-CODE", null),
        ];

        var result = ProductCreateSplitter.Split("SKU-001", "Shirt", types, variants, overrides);

        result.IsSuccess.Should().BeTrue();
        var codes = result.Value.Select(plan => plan.ModelCode).ToList();
        codes.Should().Contain("SAME-CODE");
        codes.Should().Contain(code => code.StartsWith("SKU-001-"));
    }

    [Fact]
    public void Split_WithMultipleSlicers_Fails()
    {
        var types = new[]
        {
            new ProductVariantType(ColorTypeId, "Color", SelectionStyle.Color, Slicer: true),
            new ProductVariantType(SizeTypeId, "Size", SelectionStyle.List, Slicer: true),
        };

        var result = ProductCreateSplitter.Split("SKU-001", "Shirt", types, [BasicVariant("BC-001")]);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Split_WithSlicer_MissingSlicerSelectionOnVariant_Fails()
    {
        var types = new[]
        {
            new ProductVariantType(ColorTypeId, "Color", SelectionStyle.Color, Slicer: true),
            new ProductVariantType(SizeTypeId, "Size", SelectionStyle.List),
        };

        var variants = new[]
        {
            Variant("BC-RED-S", RedValueId, "Red", SmallValueId, "S") with { VariantValues = [] },
        };

        var result = ProductCreateSplitter.Split("SKU-001", "Shirt", types, variants);
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("slicer");
    }

    private static ProductItemDraft BasicVariant(string barcode) =>
        new(null, barcode, null, null, null, null, 10m, null, 5, null, null);

    private static ProductItemDraft ColorOnlyItem(string barcode, Guid valueId, string label) =>
        new(
            null,
            barcode,
            null,
            null,
            null,
            null,
            10m,
            null,
            5,
            null,
            [
                new ProductVariantValue(
                    new ProductVariantType(ColorTypeId, "Color", SelectionStyle.Color, Slicer: true),
                    valueId,
                    label),
            ]);

    private static ProductItemDraft Variant(
        string barcode,
        Guid colorValueId,
        string colorLabel,
        Guid sizeValueId,
        string sizeLabel) =>
        new(
            null,
            barcode,
            null,
            null,
            null,
            null,
            10m,
            null,
            5,
            null,
            [
                new ProductVariantValue(
                    new ProductVariantType(ColorTypeId, "Color", SelectionStyle.Color),
                    colorValueId,
                    colorLabel),
                new ProductVariantValue(
                    new ProductVariantType(SizeTypeId, "Size", SelectionStyle.List),
                    sizeValueId,
                    sizeLabel)
            ]);
}
