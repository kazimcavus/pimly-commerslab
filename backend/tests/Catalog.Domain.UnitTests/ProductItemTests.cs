using Catalog.Domain.Products;
using FluentAssertions;

namespace Catalog.Domain.UnitTests;

/// <summary>ProductItem varlığı için birim testleri.</summary>
public class ProductItemTests
{
    private static ProductItemDraft BasicDraft() =>
        new(null, "BC-001", null, null, null, null, null, null);

    [Fact]
    public void Create_ValidDraft_Succeeds()
    {
        var result = ProductItem.Create(BasicDraft());
        result.IsSuccess.Should().BeTrue();
        result.Value.Barcode.Should().Be("BC-001");
    }

    [Fact]
    public void Update_ValidValues_Succeeds()
    {
        var variant = ProductItem.Create(BasicDraft()).Value;
        var result = variant.Update(new ProductItemUpdate("GTIN", "MPN", null, "Axis", null));
        result.IsSuccess.Should().BeTrue();
        variant.Gtin.Should().Be("GTIN");
    }

    [Fact]
    public void Create_EmptyBarcode_Fails()
    {
        var result = ProductItem.Create(BasicDraft() with { Barcode = "  " });
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("barcode");
    }
}
