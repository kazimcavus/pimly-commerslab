using System.Globalization;
using Channels.Application.Listings.OfferSync;
using FluentAssertions;

namespace Channels.UnitTests;

/// <summary>Teklif parmak izinin kararlılığı için birim testleri — delta gönderimin dayanağı.</summary>
public class OfferHasherTests
{
    private static MarketplaceOfferUpdate Offer(int quantity = 5, decimal amount = 449.90m) =>
        new("BARCODE-1", quantity, amount, 599.90m, "TRY");

    [Fact]
    public void SameOffer_ProducesSameHash() =>
        OfferHasher.Compute(Offer()).Should().Be(OfferHasher.Compute(Offer()));

    [Fact]
    public void QuantityChange_ChangesHash() =>
        OfferHasher.Compute(Offer(quantity: 5)).Should().NotBe(OfferHasher.Compute(Offer(quantity: 6)));

    [Fact]
    public void PriceChange_ChangesHash() =>
        OfferHasher.Compute(Offer(amount: 449.90m)).Should().NotBe(OfferHasher.Compute(Offer(amount: 459.90m)));

    [Fact]
    public void Hash_FitsListingColumn() =>
        OfferHasher.Compute(Offer()).Length.Should().Be(64);

    [Fact]
    public void Hash_IsCultureInvariant()
    {
        // Ondalık ayıracı virgül olan bir kültürde aynı teklif farklı hash üretirse her tur
        // gereksiz gönderim olur.
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            var turkish = OfferHasher.Compute(Offer());

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var invariant = OfferHasher.Compute(Offer());

            turkish.Should().Be(invariant);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
