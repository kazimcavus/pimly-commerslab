using Catalog.Application.Products.DeleteItemPrice;
using Catalog.Application.Products.UpsertItemPrice;
using FluentAssertions;

namespace Catalog.Application.UnitTests;

/// <summary>UpsertItemPriceCommandValidator için birim testleri.</summary>
public class UpsertItemPriceCommandValidatorTests
{
    private readonly UpsertItemPriceCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var command = new UpsertItemPriceCommand(Guid.NewGuid(), Guid.NewGuid(), 449.90m);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyItemId_Fails()
    {
        var command = new UpsertItemPriceCommand(Guid.Empty, Guid.NewGuid(), 100m);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_EmptyDefinitionId_Fails()
    {
        var command = new UpsertItemPriceCommand(Guid.NewGuid(), Guid.Empty, 100m);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_NegativeAmount_Fails()
    {
        var command = new UpsertItemPriceCommand(Guid.NewGuid(), Guid.NewGuid(), -1m);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}

/// <summary>DeleteItemPriceCommandValidator için birim testleri.</summary>
public class DeleteItemPriceCommandValidatorTests
{
    private readonly DeleteItemPriceCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var command = new DeleteItemPriceCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyDefinitionId_Fails()
    {
        var command = new DeleteItemPriceCommand(Guid.NewGuid(), Guid.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
