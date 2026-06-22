using Catalog.Domain.Products;
using FluentAssertions;

namespace Catalog.Domain.UnitTests;

/// <summary>ProductItem varlığı için birim testleri.</summary>
public class ProductItemTests
{
    private static ProductItemDraft BasicDraft(decimal price = 10m, int stock = 5) =>
        new(null, "BC-001", null, null, null, null, price, null, stock, null, null);

    [Fact]
    public void Create_NegativePrice_Fails()
    {
        var result = ProductItem.Create(BasicDraft(price: -1m));
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("price");
    }

    [Fact]
    public void Create_NegativeStock_Fails()
    {
        var result = ProductItem.Create(BasicDraft(stock: -1));
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("stock");
    }

    [Fact]
    public void Create_ValidDraft_Succeeds()
    {
        var result = ProductItem.Create(BasicDraft());
        result.IsSuccess.Should().BeTrue();
        result.Value.Barcode.Should().Be("BC-001");
        result.Value.Price.Should().Be(10m);
        result.Value.Stock.Should().Be(5);
    }

    [Fact]
    public void Update_NegativePrice_Fails()
    {
        var variant = ProductItem.Create(BasicDraft()).Value;
        var result = variant.Update(new ProductItemUpdate(null, null, null, null, -1m, null, 5, null));
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Update_ValidValues_Succeeds()
    {
        var variant = ProductItem.Create(BasicDraft()).Value;
        var result = variant.Update(new ProductItemUpdate("GTIN", "MPN", null, "Axis", 24.99m, 29.99m, 3, null));
        result.IsSuccess.Should().BeTrue();
        variant.Price.Should().Be(24.99m);
        variant.Stock.Should().Be(3);
        variant.Gtin.Should().Be("GTIN");
    }

    [Fact]
    public void Create_EmptyBarcode_Fails()
    {
        var result = ProductItem.Create(BasicDraft() with { Barcode = "  " });
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("barcode");
    }

    [Fact]
    public void Update_NegativeStock_Fails()
    {
        var variant = ProductItem.Create(BasicDraft()).Value;
        var result = variant.Update(new ProductItemUpdate(null, null, null, null, 10m, null, -1, null));
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("stock");
    }
}
