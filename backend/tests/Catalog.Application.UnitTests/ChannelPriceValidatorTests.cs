using Catalog.Application.Products.DeleteItemChannelPrice;
using Catalog.Application.Products.UpsertItemChannelPrice;
using FluentAssertions;

namespace Catalog.Application.UnitTests;

/// <summary>UpsertItemChannelPriceCommandValidator için birim testleri.</summary>
public class UpsertItemChannelPriceCommandValidatorTests
{
    private readonly UpsertItemChannelPriceCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var command = new UpsertItemChannelPriceCommand(Guid.NewGuid(), "trendyol", 449.90m, 599.90m);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyItemId_Fails()
    {
        var command = new UpsertItemChannelPriceCommand(Guid.Empty, "trendyol", 100m, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_EmptyMarketplaceKey_Fails()
    {
        var command = new UpsertItemChannelPriceCommand(Guid.NewGuid(), string.Empty, 100m, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_NegativePrice_Fails()
    {
        var command = new UpsertItemChannelPriceCommand(Guid.NewGuid(), "trendyol", -1m, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_NegativeCompareAtPrice_Fails()
    {
        var command = new UpsertItemChannelPriceCommand(Guid.NewGuid(), "trendyol", 100m, -1m);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}

/// <summary>DeleteItemChannelPriceCommandValidator için birim testleri.</summary>
public class DeleteItemChannelPriceCommandValidatorTests
{
    private readonly DeleteItemChannelPriceCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var command = new DeleteItemChannelPriceCommand(Guid.NewGuid(), "trendyol");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyMarketplaceKey_Fails()
    {
        var command = new DeleteItemChannelPriceCommand(Guid.NewGuid(), string.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
