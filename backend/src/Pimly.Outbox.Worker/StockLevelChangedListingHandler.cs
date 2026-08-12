using Channels.Application.Listings.MarkListingsDirty;
using Inventory.Domain.StockLevels.Events;
using SharedKernel;

namespace Pimly.Outbox.Worker;

/// <summary>
/// <see cref="StockLevelChanged"/> olayını dinleyip kalemin tüm pazaryeri listelemelerini "teklif kirli"
/// işaretleyen adapter. Pazaryerine çağrı yapmaz; gönderimi senkron worker'ı toplu olarak üstlenir.
/// </summary>
/// <remarks>
/// Köprü worker kompozisyon kökünde kurulur: Channels.Application, Inventory.Domain'e bağımlı kalmaz.
/// Stok tüm pazaryerlerinde ortak olduğu için pazaryeri filtresi verilmez.
/// </remarks>
internal sealed class StockLevelChangedListingHandler(
    IMarkListingsDirtyHandler markDirty,
    ILogger<StockLevelChangedListingHandler> logger) : IIntegrationEventHandler<StockLevelChanged>
{
    /// <inheritdoc/>
    public async Task HandleAsync(StockLevelChanged integrationEvent, CancellationToken cancellationToken = default)
    {
        var result = await markDirty.ExecuteAsync(
            new MarkListingsDirtyCommand(integrationEvent.ProductItemId, MarketplaceCode: null, ListingDirtyKind.Offer),
            cancellationToken);

        if (result.IsFailure)
        {
            // Hatayı fırlat ki OutboxProcessor mesajı işaretlemesin ve yeniden denesin.
            throw new InvalidOperationException(
                $"Stok değişimi listelemelere yazılamadı (ItemId={integrationEvent.ProductItemId}): {result.Error.Message}");
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "StockLevelChanged işlendi: ItemId={ItemId} için {Count} listeleme teklif-kirli işaretlendi.",
                integrationEvent.ProductItemId,
                result.Value);
        }
    }
}
