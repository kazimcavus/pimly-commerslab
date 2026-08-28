using Channels.Application.Listings.ContentSync;
using Channels.Application.Listings.OfferSync;
using Channels.Domain.Listings;
using Microsoft.Extensions.Options;
using Pimly.ListingSync.Worker.Options;
using SharedKernel.Tenancy;

namespace Pimly.ListingSync.Worker.Listings;

/// <summary>
/// Kirli listelemelerin fiyat/stok bilgisini periyodik olarak pazaryerlerine gönderen arka plan servisi.
/// </summary>
/// <remarks>
/// Import/publish worker'larıyla aynı iki-scope'lu deseni kullanır: ilk scope tenant bağlamı olmadan
/// bekleyen (tenant, pazaryeri) çiftlerini keşfeder; her çift için ikinci scope'ta ambient tenant set
/// edilip senkron çalıştırılır. Olay başına push yerine bu tur kullanıldığı için aynı kalemin ardışık
/// değişimleri tek gönderime iner.
/// </remarks>
internal sealed class ListingSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<ListingSyncWorkerOptions> options,
    ILogger<ListingSyncBackgroundService> logger) : BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollInterval = TimeSpan.FromSeconds(Math.Max(1, options.Value.PollIntervalSeconds));
        var tenantFilter = options.Value.TenantIds.ToArray();

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Listing sync worker started for tenants: {TenantIds}.",
                tenantFilter.Length > 0 ? string.Join(", ", tenantFilter) : "(all)");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(tenantFilter, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Listing sync worker iteration failed.");
            }

            await Task.Delay(pollInterval, stoppingToken);
        }
    }

    private async Task RunOnceAsync(Guid[] tenantFilter, CancellationToken cancellationToken)
    {
        IReadOnlyList<ListingSyncScope> scopes;

        // Keşif tenant bağlamı olmadan yapılır (kirli satırlar tenant'lar arası taranır).
        await using (var discoveryScope = scopeFactory.CreateAsyncScope())
        {
            var listings = discoveryScope.ServiceProvider.GetRequiredService<IProductListingRepository>();
            scopes = await listings.ListDirtyScopesAsync(
                tenantFilter.Length > 0 ? tenantFilter : null,
                TimeProvider.System.GetUtcNow(),
                cancellationToken);
        }

        foreach (var scope in scopes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var workScope = scopeFactory.CreateAsyncScope();

            if (workScope.ServiceProvider.GetService<ITenantContext>() is AmbientTenantContext tenantContext)
            {
                tenantContext.Set(scope.TenantId);
            }

            // Teklif (ucuz, onaysız) önce; içerik (pahalı, yeniden onaya girer) sonra.
            var offerResult = await workScope.ServiceProvider
                .GetRequiredService<ISyncListingOffersHandler>()
                .ExecuteAsync(scope.Marketplace.Code, cancellationToken);

            if (offerResult.IsFailure)
            {
                logger.LogWarning(
                    "Teklif senkronu başarısız (tenant {TenantId}, {Marketplace}): {Error}",
                    scope.TenantId,
                    scope.Marketplace.Code,
                    offerResult.Error.Message);
            }
            else if ((offerResult.Value.Pushed > 0 || offerResult.Value.Failed > 0)
                && logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Teklif senkronu (tenant {TenantId}, {Marketplace}): {Examined} incelendi, {Skipped} atlandı, {Pushed} gönderildi, {Failed} hata.",
                    scope.TenantId,
                    scope.Marketplace.Code,
                    offerResult.Value.Examined,
                    offerResult.Value.Skipped,
                    offerResult.Value.Pushed,
                    offerResult.Value.Failed);
            }

            var contentResult = await workScope.ServiceProvider
                .GetRequiredService<ISyncListingContentHandler>()
                .ExecuteAsync(scope.Marketplace.Code, cancellationToken);

            if (contentResult.IsFailure)
            {
                logger.LogWarning(
                    "İçerik senkronu başarısız (tenant {TenantId}, {Marketplace}): {Error}",
                    scope.TenantId,
                    scope.Marketplace.Code,
                    contentResult.Error.Message);
            }
            else if ((contentResult.Value.Created > 0 || contentResult.Value.Updated > 0 || contentResult.Value.Failed > 0)
                && logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "İçerik senkronu (tenant {TenantId}, {Marketplace}): {Examined} incelendi, {Skipped} atlandı, {Created} oluşturuldu, {Updated} güncellendi, {Failed} hata.",
                    scope.TenantId,
                    scope.Marketplace.Code,
                    contentResult.Value.Examined,
                    contentResult.Value.Skipped,
                    contentResult.Value.Created,
                    contentResult.Value.Updated,
                    contentResult.Value.Failed);
            }
        }
    }
}
