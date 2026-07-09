using Catalog.Application.Attributes.AddAttributeValue;
using Catalog.Application.Attributes.UpdateAttribute;
using Catalog.Application.Attributes.UpdateAttributeValue;
using Catalog.Application.Categories.UpdateCategory;
using Catalog.Application.Options;
using Catalog.Application.Products.UpdateProduct;
using Catalog.Application.Products.UpdateProductItem;
using Catalog.Application.Validation;
using Catalog.Application.Variants.AddVariantValue;
using Catalog.Application.Variants.UpdateVariantType;
using Catalog.Application.Variants.UpdateVariantValue;
using FluentAssertions;
using SharedKernel;
using SharedKernel.Tenancy;

namespace Catalog.Application.UnitTests;

/// <summary>UpdateProductCommandValidator için birim testleri.</summary>
public class UpdateProductCommandValidatorTests
{
    private static readonly Guid TestCategoryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly UpdateProductCommandValidator _validator = new();

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(new UpdateProductCommand(Guid.Empty, TestCategoryId, "Title", "draft", null));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public void Validate_EmptyCategoryId_Fails()
    {
        var result = _validator.Validate(new UpdateProductCommand(Guid.NewGuid(), Guid.Empty, "Title", "draft", null));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CategoryId");
    }

    [Fact]
    public void Validate_EmptyName_Fails()
    {
        var result = _validator.Validate(new UpdateProductCommand(Guid.NewGuid(), TestCategoryId, "  ", "draft", null));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_InvalidStatus_Fails()
    {
        var result = _validator.Validate(new UpdateProductCommand(Guid.NewGuid(), TestCategoryId, "Title", "invalid", null));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Status");
    }

    [Fact]
    public void Validate_ValidCommand_Succeeds()
    {
        var result = _validator.Validate(new UpdateProductCommand(Guid.NewGuid(), TestCategoryId, "Title", "active", null));
        result.IsValid.Should().BeTrue();
    }
}

/// <summary>UpdateProductItemCommandValidator için birim testleri.</summary>
public class UpdateProductItemCommandValidatorTests
{
    private readonly UpdateProductItemCommandValidator _validator = new();

    [Fact]
    public void Validate_NegativeStock_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Stock = -1 });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Stock");
    }

    [Fact]
    public void Validate_ValidCommand_Succeeds()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    private static UpdateProductItemCommand ValidCommand() =>
        new(Guid.NewGuid(), null, null, null, null, 5, null);
}

/// <summary>UpdateCategoryCommandValidator için birim testleri.</summary>
public class UpdateCategoryCommandValidatorTests
{
    private readonly UpdateCategoryCommandValidator _validator = new();

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(new UpdateCategoryCommand(Guid.Empty, "Apparel", "APP", null));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public void Validate_EmptyName_Fails()
    {
        var result = _validator.Validate(new UpdateCategoryCommand(Guid.NewGuid(), "  ", "APP", null));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_ValidCommand_Succeeds()
    {
        var result = _validator.Validate(new UpdateCategoryCommand(Guid.NewGuid(), "Apparel", "APP", null));
        result.IsValid.Should().BeTrue();
    }
}

/// <summary>UpdateAttributeCommandValidator için birim testleri.</summary>
public class UpdateAttributeCommandValidatorTests
{
    private readonly UpdateAttributeCommandValidator _validator = new();

    [Fact]
    public void Validate_EmptyName_Fails()
    {
        var result = _validator.Validate(new UpdateAttributeCommand(Guid.NewGuid(), "  "));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_ValidCommand_Succeeds()
    {
        var result = _validator.Validate(new UpdateAttributeCommand(Guid.NewGuid(), "Material"));
        result.IsValid.Should().BeTrue();
    }
}

/// <summary>UpdateAttributeValueCommandValidator için birim testleri.</summary>
public class UpdateAttributeValueCommandValidatorTests
{
    private readonly UpdateAttributeValueCommandValidator _validator = new();

    [Fact]
    public void Validate_EmptyName_Fails()
    {
        var result = _validator.Validate(new UpdateAttributeValueCommand(Guid.NewGuid(), "  "));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_ValidCommand_Succeeds()
    {
        var result = _validator.Validate(new UpdateAttributeValueCommand(Guid.NewGuid(), "Cotton"));
        result.IsValid.Should().BeTrue();
    }
}

/// <summary>AddAttributeValueCommandValidator için birim testleri.</summary>
public class AddAttributeValueCommandValidatorTests
{
    private readonly AddAttributeValueCommandValidator _validator = new();

    [Fact]
    public void Validate_EmptyName_Fails()
    {
        var result = _validator.Validate(new AddAttributeValueCommand(Guid.NewGuid(), "  "));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_ValidCommand_Succeeds()
    {
        var result = _validator.Validate(new AddAttributeValueCommand(Guid.NewGuid(), "Cotton"));
        result.IsValid.Should().BeTrue();
    }
}

/// <summary>AddVariantValueCommandValidator için birim testleri.</summary>
public class AddVariantValueCommandValidatorTests
{
    private readonly AddVariantValueCommandValidator _validator = new(
        Microsoft.Extensions.Options.Options.Create(new MediaUrlOptions { AllowedUrlPrefix = "/media/" }),
        new ValidatorTestTenantContext());

    [Fact]
    public void Validate_EmptyLabel_Fails()
    {
        var result = _validator.Validate(new AddVariantValueCommand(Guid.NewGuid(), "  ", null, null, null, 0));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Label");
    }

    [Fact]
    public void Validate_ColorTooLong_Fails()
    {
        var result = _validator.Validate(new AddVariantValueCommand(Guid.NewGuid(), "Red", new string('#', 51), null, null, 0));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Color");
    }

    [Fact]
    public void Validate_ValidCommand_Succeeds()
    {
        var result = _validator.Validate(new AddVariantValueCommand(Guid.NewGuid(), "Red", "#ff0000", null, null, 0));
        result.IsValid.Should().BeTrue();
    }
}

/// <summary>UpdateVariantTypeCommandValidator için birim testleri.</summary>
public class UpdateVariantTypeCommandValidatorTests
{
    private readonly UpdateVariantTypeCommandValidator _validator = new();

    [Fact]
    public void Validate_InvalidSelectionStyle_Fails()
    {
        var result = _validator.Validate(new UpdateVariantTypeCommand(Guid.NewGuid(), "Color", "unknown", 0, false));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SelectionStyle");
    }

    [Fact]
    public void Validate_ValidCommand_Succeeds()
    {
        var result = _validator.Validate(new UpdateVariantTypeCommand(Guid.NewGuid(), "Color", "color", 0, true));
        result.IsValid.Should().BeTrue();
    }
}

/// <summary>UpdateVariantValueCommandValidator için birim testleri.</summary>
public class UpdateVariantValueCommandValidatorTests
{
    private readonly UpdateVariantValueCommandValidator _validator = new(
        Microsoft.Extensions.Options.Options.Create(new MediaUrlOptions { AllowedUrlPrefix = "/media/" }),
        new ValidatorTestTenantContext());

    [Fact]
    public void Validate_EmptyLabel_Fails()
    {
        var result = _validator.Validate(new UpdateVariantValueCommand(Guid.NewGuid(), "  ", null, null, null, 0));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Label");
    }

    [Fact]
    public void Validate_ValidCommand_Succeeds()
    {
        var result = _validator.Validate(new UpdateVariantValueCommand(Guid.NewGuid(), "Crimson", "#dc143c", null, null, 1));
        result.IsValid.Should().BeTrue();
    }
}

internal sealed class ValidatorTestTenantContext : ITenantContext
{
    public Guid TenantId { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
}
