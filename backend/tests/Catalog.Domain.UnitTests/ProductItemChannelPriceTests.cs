using Catalog.Domain.Products;
using FluentAssertions;

namespace Catalog.Domain.UnitTests;

/// <summary>ProductItemChannelPrice aggregate için birim testleri.</summary>
public class ProductItemChannelPriceTests
{
    private static readonly Guid ItemId = Guid.NewGuid();

    [Fact]
    public void Create_ValidInput_NormalizesKeyAndCurrency()
    {
        var result = ProductItemChannelPrice.Create(ItemId, "  Trendyol ", 449.90m, 599.90m, "try");

        result.IsSuccess.Should().BeTrue();
        result.Value.MarketplaceKey.Should().Be("trendyol");
        result.Value.Currency.Should().Be("TRY");
        result.Value.Price.Should().Be(449.90m);
        result.Value.CompareAtPrice.Should().Be(599.90m);
        result.Value.ProductItemId.Should().Be(ItemId);
    }

    [Fact]
    public void Create_MissingCurrency_DefaultsToTry()
    {
        var result = ProductItemChannelPrice.Create(ItemId, "trendyol", 100m, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Currency.Should().Be("TRY");
    }

    [Fact]
    public void Create_EmptyItemId_Fails()
    {
        var result = ProductItemChannelPrice.Create(Guid.Empty, "trendyol", 100m, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Product item id");
    }

    [Fact]
    public void Create_EmptyMarketplaceKey_Fails()
    {
        var result = ProductItemChannelPrice.Create(ItemId, "  ", 100m, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Marketplace key");
    }

    [Fact]
    public void Create_TooLongMarketplaceKey_Fails()
    {
        var key = new string('a', ProductItemChannelPrice.MarketplaceKeyMaxLength + 1);
        var result = ProductItemChannelPrice.Create(ItemId, key, 100m, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Marketplace key");
    }

    [Fact]
    public void Create_NegativePrice_Fails()
    {
        var result = ProductItemChannelPrice.Create(ItemId, "trendyol", -1m, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("price");
    }

    [Fact]
    public void Create_NegativeCompareAtPrice_Fails()
    {
        var result = ProductItemChannelPrice.Create(ItemId, "trendyol", 100m, -5m);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("compare at");
    }

    [Fact]
    public void UpdatePrice_ValidInput_UpdatesFieldsAndTimestamp()
    {
        var channelPrice = ProductItemChannelPrice.Create(ItemId, "trendyol", 100m, null).Value;
        var initialUpdatedAt = channelPrice.UpdatedAt;

        var result = channelPrice.UpdatePrice(129.99m, 149.99m, "usd");

        result.IsSuccess.Should().BeTrue();
        channelPrice.Price.Should().Be(129.99m);
        channelPrice.CompareAtPrice.Should().Be(149.99m);
        channelPrice.Currency.Should().Be("USD");
        channelPrice.UpdatedAt.Should().BeOnOrAfter(initialUpdatedAt);
    }

    [Fact]
    public void UpdatePrice_NegativePrice_FailsWithoutChanges()
    {
        var channelPrice = ProductItemChannelPrice.Create(ItemId, "trendyol", 100m, null).Value;

        var result = channelPrice.UpdatePrice(-10m, null);

        result.IsFailure.Should().BeTrue();
        channelPrice.Price.Should().Be(100m);
    }
}
