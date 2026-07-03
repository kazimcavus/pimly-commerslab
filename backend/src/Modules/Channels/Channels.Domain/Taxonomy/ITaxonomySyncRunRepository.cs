using Channels.Domain.Marketplaces;

namespace Channels.Domain.Taxonomy;

/// <summary>TaxonomySyncRun aggregate depo arabirimi.</summary>
public interface ITaxonomySyncRunRepository
{
    Task<TaxonomySyncRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TaxonomySyncRun?> GetActiveForMarketplaceAsync(
        MarketplaceKey marketplaceKey,
        CancellationToken cancellationToken = default);

    Task<TaxonomySyncRun?> GetLatestCompletedForMarketplaceAsync(
        MarketplaceKey marketplaceKey,
        CancellationToken cancellationToken = default);

    Task<bool> HasSyncRunSinceAsync(
        MarketplaceKey marketplaceKey,
        DateTimeOffset since,
        CancellationToken cancellationToken = default);

    /// <summary>Bir pending job'ı atomik olarak claim eder ve running yapar.</summary>
    Task<TaxonomySyncRun?> TryClaimNextPendingAsync(CancellationToken cancellationToken = default);

    Task AddAsync(TaxonomySyncRun syncRun, CancellationToken cancellationToken = default);

    void Update(TaxonomySyncRun syncRun);
}
