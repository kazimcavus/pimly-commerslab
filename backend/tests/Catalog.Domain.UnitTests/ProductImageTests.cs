using Catalog.Domain.Products;
using FluentAssertions;

namespace Catalog.Domain.UnitTests;

/// <summary>ProductImage aggregate davranışı için birim testleri.</summary>
public class ProductImageTests
{
    [Fact]
    public void AddImage_WithPrimary_ClearsExistingPrimary()
    {
        var product = CreateProductWithItem();
        product.AddImage("/media/a/a/first.jpg", 0, null, true, null).IsSuccess.Should().BeTrue();
        product.AddImage("/media/a/b/second.jpg", 1, null, true, null).IsSuccess.Should().BeTrue();

        product.Images.Should().HaveCount(2);
        product.Images.Single(i => i.Url.EndsWith("first.jpg", StringComparison.Ordinal)).IsPrimary.Should().BeFalse();
        product.Images.Single(i => i.Url.EndsWith("second.jpg", StringComparison.Ordinal)).IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void SetPrimaryImage_SwitchesPrimaryFlag()
    {
        var product = CreateProductWithItem();
        var first = product.AddImage("/media/a/a/first.jpg", 0, null, true, null).Value;
        var second = product.AddImage("/media/a/b/second.jpg", 1, null, false, null).Value;

        product.SetPrimaryImage(second.Id).IsSuccess.Should().BeTrue();

        product.Images.Single(i => i.Id == first.Id).IsPrimary.Should().BeFalse();
        product.Images.Single(i => i.Id == second.Id).IsPrimary.Should().BeTrue();
    }

    private static Product CreateProductWithItem()
    {
        var product = Product.Create(
            Guid.NewGuid(),
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "TEST-001",
            "Test Product",
            ProductStatus.Draft,
            [],
            [],
            [new ProductItemDraft(null, "8690000001", null, null, null, null, 10m, null, 1, [], [])]).Value;

        return product;
    }
}
