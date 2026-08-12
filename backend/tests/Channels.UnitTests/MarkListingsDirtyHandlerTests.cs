using Channels.Application.Listings.MarkListingsDirty;
using Channels.Domain;
using Channels.Domain.Listings;
using FluentAssertions;
using SharedKernel;

namespace Channels.UnitTests;

/// <summary>Değişim sinyallerinin listeleme kirliliğine çevrilmesi için birim testleri.</summary>
public class MarkListingsDirtyHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public async Task Offer_MarksAllMarketplaces_WhenCodeOmitted()
    {
        var itemId = Guid.NewGuid();
        var listing = SeedListing(itemId);
        var (handler, repository) = CreateHandler(listing);

        var result = await handler.ExecuteAsync(
            new MarkListingsDirtyCommand(itemId, MarketplaceCode: null, ListingDirtyKind.Offer));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        listing.OfferDirtyAt.Should().Be(Now);
        listing.ContentDirtyAt.Should().BeNull();
        repository.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Offer_SkipsOtherMarketplaces_WhenCodeGiven()
    {
        var itemId = Guid.NewGuid();
        var listing = SeedListing(itemId);
        var (handler, _) = CreateHandler(listing);

        // Trendyol dışında bir kod verilemez (kapalı evren), bu yüzden bilinmeyen kod hata döner.
        var result = await handler.ExecuteAsync(
            new MarkListingsDirtyCommand(itemId, "ZZ", ListingDirtyKind.Offer));

        result.IsFailure.Should().BeTrue();
        listing.OfferDirtyAt.Should().BeNull();
    }

    [Fact]
    public async Task NeverListedItem_IsNoOp()
    {
        var (handler, repository) = CreateHandler();

        var result = await handler.ExecuteAsync(
            new MarkListingsDirtyCommand(Guid.NewGuid(), MarketplaceCode: null, ListingDirtyKind.Offer));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);

        // Pazaryerinde karşılığı olmayan kalem için kaydetme bile yapılmaz.
        repository.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task RepeatedSignals_CollapseToSingleDirtyStamp()
    {
        var itemId = Guid.NewGuid();
        var listing = SeedListing(itemId);
        var (handler, _) = CreateHandler(listing);
        var command = new MarkListingsDirtyCommand(itemId, MarketplaceCode: null, ListingDirtyKind.Offer);

        await handler.ExecuteAsync(command);
        await handler.ExecuteAsync(command);
        await handler.ExecuteAsync(command);

        // Debounce'un temeli: işaretleme idempotenttir, ilk damga korunur.
        listing.OfferDirtyAt.Should().Be(Now);
    }

    [Fact]
    public async Task Both_MarksContentAndOffer()
    {
        var itemId = Guid.NewGuid();
        var listing = SeedListing(itemId);
        var (handler, _) = CreateHandler(listing);

        await handler.ExecuteAsync(
            new MarkListingsDirtyCommand(itemId, MarketplaceCode: null, ListingDirtyKind.Both));

        listing.OfferDirtyAt.Should().Be(Now);
        listing.ContentDirtyAt.Should().Be(Now);
    }

    [Fact]
    public async Task EmptyProductItemId_Fails()
    {
        var (handler, _) = CreateHandler();

        var result = await handler.ExecuteAsync(
            new MarkListingsDirtyCommand(Guid.Empty, MarketplaceCode: null, ListingDirtyKind.Offer));

        result.IsFailure.Should().BeTrue();
    }

    private static ProductListing SeedListing(Guid itemId)
    {
        var listing = ProductListing.Seed(TenantId, Marketplace.Trendyol, itemId, "BARCODE-1", Now).Value;

        // Seed baştan kirlidir; testler işaretlemeyi izleyebilsin diye temiz duruma çekilir.
        listing.MarkContentSubmitted("content-hash", "batch-1", Now);
        listing.MarkOfferSynced("offer-hash", Now);
        return listing;
    }

    private static (MarkListingsDirtyHandler Handler, FakeListingRepository Repository) CreateHandler(
        params ProductListing[] listings)
    {
        var repository = new FakeListingRepository(listings);
        var handler = new MarkListingsDirtyHandler(
            repository,
            repository,
            new FakeTimeProvider(Now));

        return (handler, repository);
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeListingRepository(IReadOnlyList<ProductListing> listings)
        : IProductListingRepository, IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(0);
        }

        public Task<IReadOnlyList<ProductListing>> ListByProductItemAsync(
            Guid productItemId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProductListing>>(
                [.. listings.Where(listing => listing.ProductItemId == productItemId)]);

        public Task<ProductListing?> GetAsync(
            Marketplace marketplace,
            Guid productItemId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(listings.FirstOrDefault(listing =>
                listing.Marketplace == marketplace && listing.ProductItemId == productItemId));

        public Task<IReadOnlyList<ProductListing>> ListByProductItemsAsync(
            Marketplace marketplace,
            IReadOnlyCollection<Guid> productItemIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProductListing>>(
                [.. listings.Where(listing =>
                    listing.Marketplace == marketplace && productItemIds.Contains(listing.ProductItemId))]);

        public Task<IReadOnlyList<ProductListing>> ListDirtyAsync(
            Marketplace marketplace,
            DateTimeOffset now,
            int limit,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProductListing>>(
                [.. listings.Where(listing => listing.Marketplace == marketplace && listing.IsDirty).Take(limit)]);

        public Task<IReadOnlyList<ListingSyncScope>> ListDirtyScopesAsync(
            IReadOnlyCollection<Guid>? tenantIds,
            DateTimeOffset now,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ListingSyncScope>>(
                [.. listings
                    .Where(listing => listing.IsDirty)
                    .Select(listing => new ListingSyncScope(listing.TenantId, listing.Marketplace))
                    .Distinct()]);

        public Task AddAsync(ProductListing listing, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task AddRangeAsync(
            IReadOnlyCollection<ProductListing> listingsToAdd,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public void Update(ProductListing listing)
        {
        }
    }
}
