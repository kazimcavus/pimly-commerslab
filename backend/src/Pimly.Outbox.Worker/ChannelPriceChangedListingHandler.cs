using Channels.Application.Listings.MarkListingsDirty;
using Pricing.Domain.ChannelPrices.Events;
using SharedKernel;

namespace Pimly.Outbox.Worker;

/// <summary>
/// <see cref="ChannelPriceChanged"/> olayını dinleyip yalnızca ilgili pazaryerinin listelemesini
/// "teklif kirli" işaretleyen adapter.
/// </summary>
/// <remarks>
/// Fiyat pazaryerine özgü karar olduğu için kirlilik yalnızca o pazaryerine yazılır; diğer kanalların
/// listelemeleri gereksiz yere gönderime girmez.
/// </remarks>
internal sealed class ChannelPriceChangedListingHandler(
    IMarkListingsDirtyHandler markDirty,
    ILogger<ChannelPriceChangedListingHandler> logger) : IIntegrationEventHandler<ChannelPriceChanged>
{
    /// <inheritdoc/>
    public async Task HandleAsync(ChannelPriceChanged integrationEvent, CancellationToken cancellationToken = default)
    {
        var result = await markDirty.ExecuteAsync(
            new MarkListingsDirtyCommand(
                integrationEvent.ProductItemId,
                integrationEvent.MarketplaceCode,
                ListingDirtyKind.Offer),
            cancellationToken);

        if (result.IsFailure)
        {
            // Hatayı fırlat ki OutboxProcessor mesajı işaretlemesin ve yeniden denesin.
            throw new InvalidOperationException(
                $"Kanal fiyatı değişimi listelemelere yazılamadı (ItemId={integrationEvent.ProductItemId}): {result.Error.Message}");
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "ChannelPriceChanged işlendi: ItemId={ItemId}, Pazaryeri={Marketplace} için {Count} listeleme teklif-kirli işaretlendi.",
                integrationEvent.ProductItemId,
                integrationEvent.MarketplaceCode,
                result.Value);
        }
    }
}
