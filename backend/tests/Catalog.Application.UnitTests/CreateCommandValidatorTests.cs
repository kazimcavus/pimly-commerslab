using Catalog.Application.Attributes.CreateAttribute;
using Catalog.Application.Categories.CreateCategory;
using Catalog.Application.Products.CreateProduct;
using Catalog.Application.Products.CreateProductsBatch;
using Catalog.Application.Variants.CreateVariantType;
using Catalog.Domain.Products;
using FluentAssertions;
using SharedKernel;

namespace Catalog.Application.UnitTests;

/// <summary>CreateCategoryCommandValidator için smoke testleri.</summary>
public class CreateCategoryCommandValidatorTests
{
    private readonly CreateCategoryCommandValidator _validator = new();

    [Fact]
    public void Validate_EmptyName_Fails()
    {
        var result = _validator.Validate(new CreateCategoryCommand("  ", null, null));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Fact]
    public void Validate_ValidCommand_Succeeds()
    {
        var result = _validator.Validate(new CreateCategoryCommand("Apparel", "APP", null));
        result.IsValid.Should().BeTrue();
    }
}

/// <summary>CreateProductCommandValidator için smoke testleri.</summary>
public class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    [Fact]
    public void Validate_EmptyCategoryId_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { CategoryId = Guid.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CategoryId");
    }

    [Fact]
    public void Validate_EmptyGroupId_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { GroupId = Guid.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "GroupId");
    }

    [Fact]
    public void Validate_WhitespaceModelCode_AllowedAtValidationLayer()
    {
        var result = _validator.Validate(ValidCommand() with { ModelCode = "  " });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidStatus_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Status = "invalid" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Status");
    }

    [Fact]
    public void Validate_EmptyItems_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Items = [] });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Items");
    }

    private static CreateProductCommand ValidCommand() =>
        new(
            Guid.NewGuid(),
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "SKU-001",
            "Title",
            "draft",
            null,
            null,
            [],
            [ValidItem()]);

    private static CreateProductItemInput ValidItem() =>
        new(null, "8690000001", null, null, null, null, null, null);
}

/// <summary>CreateProductsBatchCommandValidator için smoke testleri.</summary>
public class CreateProductsBatchCommandValidatorTests
{
    private readonly CreateProductsBatchCommandValidator _validator = new();

    [Fact]
    public void Validate_EmptyProducts_Fails()
    {
        var result = _validator.Validate(new CreateProductsBatchCommand(Guid.NewGuid(), []));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Products");
    }

    [Fact]
    public void Validate_ValidCommand_Succeeds()
    {
        var item = new CreateProductsBatchItem(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "SKU-001",
            "Title",
            "draft",
            null,
            null,
            [],
            [new CreateProductItemInput(null, "8690000001", null, null, null, null, null, null)]);

        var result = _validator.Validate(new CreateProductsBatchCommand(Guid.NewGuid(), [item]));
        result.IsValid.Should().BeTrue();
    }
}

/// <summary>CreateAttributeCommandValidator için smoke testleri.</summary>
public class CreateAttributeCommandValidatorTests
{
    private readonly CreateAttributeCommandValidator _validator = new();

    [Fact]
    public void Validate_EmptyName_Fails()
    {
        var result = _validator.Validate(new CreateAttributeCommand("  "));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_ValidCommand_Succeeds()
    {
        var result = _validator.Validate(new CreateAttributeCommand("Material"));
        result.IsValid.Should().BeTrue();
    }
}

/// <summary>CreateVariantTypeCommandValidator için smoke testleri.</summary>
public class CreateVariantTypeCommandValidatorTests
{
    private readonly CreateVariantTypeCommandValidator _validator = new();

    [Fact]
    public void Validate_EmptyName_Fails()
    {
        var result = _validator.Validate(new CreateVariantTypeCommand("  ", "list", 0));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_InvalidSelectionStyle_Fails()
    {
        var result = _validator.Validate(new CreateVariantTypeCommand("Color", "unknown", 0));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SelectionStyle");
    }

    [Fact]
    public void Validate_ValidCommand_Succeeds()
    {
        var result = _validator.Validate(new CreateVariantTypeCommand("Color", "color", 0, Slicer: true));
        result.IsValid.Should().BeTrue();
    }
}
