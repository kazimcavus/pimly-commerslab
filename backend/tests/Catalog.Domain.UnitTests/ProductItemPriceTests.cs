using Catalog.Domain.Products;
using FluentAssertions;

namespace Catalog.Domain.UnitTests;

/// <summary>ProductItemPrice aggregate için birim testleri.</summary>
public class ProductItemPriceTests
{
    private static readonly Guid ItemId = Guid.NewGuid();
    private static readonly Guid DefinitionId = Guid.NewGuid();

    [Fact]
    public void Create_ValidInput_NormalizesCurrency()
    {
        var result = ProductItemPrice.Create(ItemId, DefinitionId, 449.90m, "try");

        result.IsSuccess.Should().BeTrue();
        result.Value.ProductItemId.Should().Be(ItemId);
        result.Value.PriceDefinitionId.Should().Be(DefinitionId);
        result.Value.Amount.Should().Be(449.90m);
        result.Value.Currency.Should().Be("TRY");
    }

    [Fact]
    public void Create_MissingCurrency_DefaultsToTry()
    {
        var result = ProductItemPrice.Create(ItemId, DefinitionId, 100m);

        result.IsSuccess.Should().BeTrue();
        result.Value.Currency.Should().Be("TRY");
    }

    [Fact]
    public void Create_EmptyItemId_Fails()
    {
        var result = ProductItemPrice.Create(Guid.Empty, DefinitionId, 100m);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Product item id");
    }

    [Fact]
    public void Create_EmptyDefinitionId_Fails()
    {
        var result = ProductItemPrice.Create(ItemId, Guid.Empty, 100m);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Price definition id");
    }

    [Fact]
    public void Create_NegativeAmount_Fails()
    {
        var result = ProductItemPrice.Create(ItemId, DefinitionId, -1m);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("amount");
    }

    [Fact]
    public void UpdateAmount_ValidInput_UpdatesFieldsAndTimestamp()
    {
        var itemPrice = ProductItemPrice.Create(ItemId, DefinitionId, 100m).Value;
        var initialUpdatedAt = itemPrice.UpdatedAt;

        var result = itemPrice.UpdateAmount(129.99m, "usd");

        result.IsSuccess.Should().BeTrue();
        itemPrice.Amount.Should().Be(129.99m);
        itemPrice.Currency.Should().Be("USD");
        itemPrice.UpdatedAt.Should().BeOnOrAfter(initialUpdatedAt);
    }

    [Fact]
    public void UpdateAmount_NegativeAmount_FailsWithoutChanges()
    {
        var itemPrice = ProductItemPrice.Create(ItemId, DefinitionId, 100m).Value;

        var result = itemPrice.UpdateAmount(-10m);

        result.IsFailure.Should().BeTrue();
        itemPrice.Amount.Should().Be(100m);
    }
}
