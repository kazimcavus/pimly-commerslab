using Catalog.Domain;
using Catalog.Domain.Products;
using Catalog.Domain.Variants;
using FluentAssertions;
using ProductVariantType = Catalog.Domain.Products.Variant;

namespace Catalog.Domain.UnitTests;

/// <summary>Product aggregate kökü için birim testleri.</summary>
public class ProductTests
{
    private static readonly Guid TestCategoryId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static ProductItemDraft BasicVariant(string barcode = "BC-001") =>
        new(null, barcode, null, null, null, null, 10m, null, 5, null, null);

    [Fact]
    public void Create_WithEmptySku_Fails()
    {
        var result = Product.Create(
            Guid.NewGuid(),
            TestCategoryId,
            "  ",
            "Title",
            ProductStatus.Draft,
            null,
            null,
            [BasicVariant()]);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_BasicProduct_WithMultipleVariants_Fails()
    {
        var result = Product.Create(
            Guid.NewGuid(),
            TestCategoryId,
            "SKU-001",
            "Basic",
            ProductStatus.Draft,
            null,
            [],
            [BasicVariant("BC-001"), BasicVariant("BC-002")]);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("exactly one variant");
    }

    [Fact]
    public void Create_BasicProduct_WithSingleVariant_Succeeds()
    {
        var result = Product.Create(
            Guid.NewGuid(),
            TestCategoryId,
            "SKU-001",
            "Basic",
            ProductStatus.Draft,
            null,
            [],
            [BasicVariant()]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
    }

    [Fact]
    public void Create_VariantProduct_WithNoVariants_Fails()
    {
        var variantTypes = new[] { new ProductVariantType(Guid.Parse("00000000-0000-0000-0000-000000000001"), "Color", SelectionStyle.Color) };
        var result = Product.Create(
            Guid.NewGuid(),
            TestCategoryId,
            "SKU-002",
            "Variant",
            ProductStatus.Draft,
            null,
            variantTypes,
            []);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("at least one variant");
    }

    [Fact]
    public void Create_VariantProduct_WithTooManyTypes_Fails()
    {
        var variantTypes = new[]
        {
            new ProductVariantType(Guid.Parse("00000000-0000-0000-0000-000000000001"), "A", SelectionStyle.List),
            new ProductVariantType(Guid.Parse("00000000-0000-0000-0000-000000000002"), "B", SelectionStyle.List),
            new ProductVariantType(Guid.Parse("00000000-0000-0000-0000-000000000003"), "C", SelectionStyle.List),
            new ProductVariantType(Guid.Parse("00000000-0000-0000-0000-000000000004"), "D", SelectionStyle.List),
        };
        var result = Product.Create(
            Guid.NewGuid(),
            TestCategoryId,
            "SKU-003",
            "Variant",
            ProductStatus.Draft,
            null,
            variantTypes,
            [BasicVariant()]);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("between 0 and 3 variant types");
    }

    [Fact]
    public void RemoveItem_FromBasicProduct_Fails()
    {
        var product = Product.Create(
            Guid.NewGuid(),
            TestCategoryId,
            "SKU-004",
            "Basic",
            ProductStatus.Draft,
            null,
            [],
            [BasicVariant()]).Value;

        var itemId = product.Items.First().Id;
        var result = product.RemoveItem(itemId);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("exactly one variant");
    }

    [Fact]
    public void Create_WithEmptyBarcode_Fails()
    {
        var result = Product.Create(
            Guid.NewGuid(),
            TestCategoryId,
            "SKU-005",
            "Basic",
            ProductStatus.Draft,
            null,
            [],
            [BasicVariant("  ")]);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void UpdateDetails_EmptyName_Fails()
    {
        var product = Product.Create(
            Guid.NewGuid(),
            TestCategoryId,
            "SKU-006",
            "Basic",
            ProductStatus.Draft,
            null,
            [],
            [BasicVariant()]).Value;

        var result = product.UpdateDetails(TestCategoryId, "  ", ProductStatus.Active, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_WithEmptyCategoryId_Fails()
    {
        var result = Product.Create(
            Guid.NewGuid(),
            Guid.Empty,
            "SKU-001",
            "Basic",
            ProductStatus.Draft,
            null,
            [],
            [BasicVariant()]);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Category id is required");
    }

    [Fact]
    public void UpdateDetails_EmptyCategoryId_Fails()
    {
        var product = Product.Create(
            Guid.NewGuid(),
            TestCategoryId,
            "SKU-010",
            "Basic",
            ProductStatus.Draft,
            null,
            [],
            [BasicVariant()]).Value;

        var result = product.UpdateDetails(Guid.Empty, "Updated Title", ProductStatus.Active, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Category id is required");
    }

    [Fact]
    public void UpdateDetails_ValidValues_Succeeds()
    {
        var product = Product.Create(
            Guid.NewGuid(),
            TestCategoryId,
            "SKU-007",
            "Basic",
            ProductStatus.Draft,
            null,
            [],
            [BasicVariant()]).Value;

        var newCategoryId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var result = product.UpdateDetails(newCategoryId, "Updated Title", ProductStatus.Active, null);

        result.IsSuccess.Should().BeTrue();
        product.Name.Should().Be("Updated Title");
        product.Status.Should().Be(ProductStatus.Active);
        product.CategoryId.Should().Be(newCategoryId);
    }

    [Fact]
    public void UpdateItem_UnknownItem_Fails()
    {
        var product = Product.Create(
            Guid.NewGuid(),
            TestCategoryId,
            "SKU-008",
            "Basic",
            ProductStatus.Draft,
            null,
            [],
            [BasicVariant()]).Value;

        var result = product.UpdateItem(Guid.NewGuid(), new ProductItemUpdate(null, null, null, null, 10m, null, 5, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("not_found");
    }

    [Fact]
    public void RemoveItem_FromVariantProduct_WithTwoItems_Succeeds()
    {
        var sizeType = new ProductVariantType(Guid.Parse("00000000-0000-0000-0000-000000000001"), "Size", SelectionStyle.List);
        var product = Product.Create(
            Guid.NewGuid(),
            TestCategoryId,
            "SKU-009",
            "Variant",
            ProductStatus.Draft,
            null,
            [sizeType],
            [BasicVariant("BC-001"), BasicVariant("BC-002")]).Value;

        var itemToRemove = product.Items.First().Id;
        var result = product.RemoveItem(itemToRemove);

        result.IsSuccess.Should().BeTrue();
        product.Items.Should().HaveCount(1);
        product.Items.Should().NotContain(i => i.Id == itemToRemove);
    }
}
