using Channels.Application.Listings.ContentSync;
using FluentAssertions;

namespace Channels.UnitTests;

/// <summary>İçerik parmak izinin kararlılığı ve kapsamı için birim testleri.</summary>
public class ContentHasherTests
{
    private static MarketplaceListingRequest Listing(
        string title = "Klasik Gömlek",
        decimal amount = 449.90m,
        int quantity = 5,
        IReadOnlyList<MarketplaceListingAttribute>? attributes = null,
        IReadOnlyList<string>? images = null) =>
        new(
            Guid.Empty,
            "BARCODE-1",
            title,
            "Açıklama",
            "1234",
            "77",
            "Marka",
            "MODEL-1",
            "SKU-1",
            amount,
            599.90m,
            "TRY",
            quantity,
            attributes ?? [new MarketplaceListingAttribute("10", "20", null)],
            images ?? ["https://cdn/1.jpg", "https://cdn/2.jpg"]);

    [Fact]
    public void SameContent_ProducesSameHash() =>
        ContentHasher.Compute(Listing()).Should().Be(ContentHasher.Compute(Listing()));

    [Fact]
    public void PriceChange_DoesNotChangeHash()
    {
        // Kritik: fiyat değişimi içerik gönderimini tetiklememeli — yoksa ürün gereksiz yere
        // pazaryerinde yeniden onaya girer ve geçici olarak satıştan düşer.
        ContentHasher.Compute(Listing(amount: 449.90m))
            .Should().Be(ContentHasher.Compute(Listing(amount: 399.90m)));
    }

    [Fact]
    public void QuantityChange_DoesNotChangeHash() =>
        ContentHasher.Compute(Listing(quantity: 5))
            .Should().Be(ContentHasher.Compute(Listing(quantity: 0)));

    [Fact]
    public void TitleChange_ChangesHash() =>
        ContentHasher.Compute(Listing(title: "Klasik Gömlek"))
            .Should().NotBe(ContentHasher.Compute(Listing(title: "Slim Fit Gömlek")));

    [Fact]
    public void ImageChange_ChangesHash() =>
        ContentHasher.Compute(Listing(images: ["https://cdn/1.jpg"]))
            .Should().NotBe(ContentHasher.Compute(Listing(images: ["https://cdn/1.jpg", "https://cdn/2.jpg"])));

    [Fact]
    public void AttributeOrder_DoesNotChangeHash()
    {
        var ascending = Listing(attributes:
        [
            new MarketplaceListingAttribute("10", "20", null),
            new MarketplaceListingAttribute("11", "21", null),
        ]);

        var descending = Listing(attributes:
        [
            new MarketplaceListingAttribute("11", "21", null),
            new MarketplaceListingAttribute("10", "20", null),
        ]);

        ContentHasher.Compute(ascending).Should().Be(ContentHasher.Compute(descending));
    }

    [Fact]
    public void AttributeValueChange_ChangesHash()
    {
        var before = Listing(attributes: [new MarketplaceListingAttribute("10", "20", null)]);
        var after = Listing(attributes: [new MarketplaceListingAttribute("10", "21", null)]);

        ContentHasher.Compute(before).Should().NotBe(ContentHasher.Compute(after));
    }

    [Fact]
    public void Hash_FitsListingColumn() =>
        ContentHasher.Compute(Listing()).Length.Should().Be(64);
}
