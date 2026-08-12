using Channels.Domain.Listings;
using FluentAssertions;
using SharedKernel;

namespace Channels.UnitTests;

/// <summary>ProductListing durum makinesi, kirlilik izleme ve delta atlama kuralları için birim testleri.</summary>
public class ProductListingTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 9, 0, 0, TimeSpan.Zero);

    private static ProductListing CreateListing() =>
        ProductListing.Create(Guid.NewGuid(), Marketplace.Trendyol, Guid.NewGuid(), Now).Value;

    private static ProductListing SeedListing() =>
        ProductListing.Seed(Guid.NewGuid(), Marketplace.Trendyol, Guid.NewGuid(), "TY-BARCODE-1", Now).Value;

    [Fact]
    public void Create_EmptyTenant_Fails() =>
        ProductListing.Create(Guid.Empty, Marketplace.Trendyol, Guid.NewGuid(), Now)
            .IsFailure.Should().BeTrue();

    [Fact]
    public void Create_EmptyProductItem_Fails() =>
        ProductListing.Create(Guid.NewGuid(), Marketplace.Trendyol, Guid.Empty, Now)
            .IsFailure.Should().BeTrue();

    [Fact]
    public void Create_StartsPendingAndDirty()
    {
        var listing = CreateListing();

        listing.Status.Should().Be(ListingStatus.Pending);
        listing.ExternalListingId.Should().BeNull();
        listing.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void Seed_StartsLiveWithExternalIdAndDirty()
    {
        var listing = SeedListing();

        listing.Status.Should().Be(ListingStatus.Live);
        listing.ExternalListingId.Should().Be("TY-BARCODE-1");
        listing.LastConfirmedAt.Should().Be(Now);

        // Hash'ler bilinmiyor: ilk senkron turunda kanonik veri pazaryerine uzlaştırılmalı.
        listing.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void Seed_BlankExternalId_Fails() =>
        ProductListing.Seed(Guid.NewGuid(), Marketplace.Trendyol, Guid.NewGuid(), "  ", Now)
            .IsFailure.Should().BeTrue();

    [Fact]
    public void MarkContentDirty_IsIdempotent_KeepsFirstStamp()
    {
        var listing = SeedListing();
        listing.MarkContentSubmitted("hash-a", "batch-1", Now);

        listing.MarkContentDirty(Now.AddMinutes(1));
        listing.MarkContentDirty(Now.AddMinutes(5));

        listing.ContentDirtyAt.Should().Be(Now.AddMinutes(1));
    }

    [Fact]
    public void NeedsOfferSync_SameHash_SkipsCall()
    {
        var listing = SeedListing();
        listing.MarkOfferSynced("offer-hash", Now).IsSuccess.Should().BeTrue();

        listing.NeedsOfferSync("offer-hash").Should().BeFalse();
        listing.NeedsOfferSync("offer-hash-2").Should().BeTrue();
    }

    [Fact]
    public void NeedsOfferSync_WithoutExternalId_IsFalse()
    {
        // Pazaryerinde karşılığı olmayan kaleme fiyat/stok gönderilemez.
        var listing = CreateListing();

        listing.NeedsOfferSync("offer-hash").Should().BeFalse();
    }

    [Fact]
    public void MarkOfferSynced_WithoutExternalId_Conflicts() =>
        CreateListing().MarkOfferSynced("offer-hash", Now).IsFailure.Should().BeTrue();

    [Fact]
    public void MarkOfferSynced_KeepsLiveStatus()
    {
        var listing = SeedListing();

        listing.MarkOfferSynced("offer-hash", Now);

        // Teklif güncellemesi yeniden onay tetiklemez; canlı listeleme canlı kalır.
        listing.Status.Should().Be(ListingStatus.Live);
        listing.OfferDirtyAt.Should().BeNull();
    }

    [Fact]
    public void MarkContentSubmitted_ClearsDirtyAndAwaitsApproval()
    {
        var listing = SeedListing();

        listing.MarkContentSubmitted("content-hash", "batch-9", Now).IsSuccess.Should().BeTrue();

        listing.Status.Should().Be(ListingStatus.Submitted);
        listing.ContentDirtyAt.Should().BeNull();
        listing.ContentHash.Should().Be("content-hash");
        listing.SubmissionReference.Should().Be("batch-9");
    }

    [Fact]
    public void MarkRejected_ClearsHashSoCorrectionIsResent()
    {
        var listing = SeedListing();
        listing.MarkContentSubmitted("content-hash", "batch-9", Now);

        listing.MarkRejected("Görsel çözünürlüğü yetersiz.", Now.AddMinutes(10)).IsSuccess.Should().BeTrue();

        listing.Status.Should().Be(ListingStatus.Rejected);
        listing.RejectionReason.Should().Be("Görsel çözünürlüğü yetersiz.");

        // Saklanan hash artık pazaryerindeki durumu temsil etmiyor: aynı içerik bile yeniden gitmeli.
        listing.ContentHash.Should().BeNull();
        listing.NeedsContentSync("content-hash").Should().BeTrue();
    }

    [Fact]
    public void RegisterSyncFailure_KeepsDirtyAndDefersRetry()
    {
        var listing = SeedListing();
        listing.MarkContentDirty(Now);

        listing.RegisterSyncFailure(Now.AddMinutes(2));

        // Taşıma hatası durumu değiştirmez; kirlilik korunur, bir sonraki tur doğal olarak yeniden dener.
        listing.Status.Should().Be(ListingStatus.Live);
        listing.IsDirty.Should().BeTrue();
        listing.SyncAttempts.Should().Be(1);
        listing.IsSyncDue(Now.AddMinutes(1)).Should().BeFalse();
        listing.IsSyncDue(Now.AddMinutes(3)).Should().BeTrue();
    }

    [Fact]
    public void SuccessfulSync_ResetsBackoff()
    {
        var listing = SeedListing();
        listing.RegisterSyncFailure(Now.AddMinutes(5));

        listing.MarkOfferSynced("offer-hash", Now.AddMinutes(6));

        listing.SyncAttempts.Should().Be(0);
        listing.NextAttemptAt.Should().BeNull();
    }

    [Fact]
    public void MarkLive_SetsExternalIdAndClearsRejection()
    {
        var listing = CreateListing();
        listing.MarkContentSubmitted("content-hash", "batch-1", Now);

        listing.MarkLive("TY-999", Now.AddMinutes(30)).IsSuccess.Should().BeTrue();

        listing.Status.Should().Be(ListingStatus.Live);
        listing.ExternalListingId.Should().Be("TY-999");
        listing.RejectionReason.Should().BeNull();
    }

    [Fact]
    public void RequestDelist_StopsFurtherSync()
    {
        var listing = SeedListing();
        listing.MarkContentDirty(Now);

        listing.RequestDelist().IsSuccess.Should().BeTrue();

        listing.Status.Should().Be(ListingStatus.PendingDelist);
        listing.IsDirty.Should().BeFalse();
        listing.NeedsContentSync("any-hash").Should().BeFalse();
        listing.NeedsOfferSync("any-hash").Should().BeFalse();
    }

    [Fact]
    public void MarkDelisted_RequiresPendingDelist()
    {
        var listing = SeedListing();

        listing.MarkDelisted(Now).IsFailure.Should().BeTrue();

        listing.RequestDelist();
        listing.MarkDelisted(Now).IsSuccess.Should().BeTrue();
        listing.Status.Should().Be(ListingStatus.Delisted);
    }
}
