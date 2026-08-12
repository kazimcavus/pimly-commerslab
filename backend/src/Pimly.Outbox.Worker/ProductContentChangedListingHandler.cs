using Catalog.Domain.Products.Events;
using Channels.Application.Listings.MarkListingsDirty;
using SharedKernel;

namespace Pimly.Outbox.Worker;

/// <summary>
/// <see cref="ProductContentChanged"/> olayını dinleyip etkilenen kalemlerin listelemelerini
/// "içerik kirli" işaretleyen adapter.
/// </summary>
/// <remarks>
/// İçerik gönderimi pazaryerinde yeniden onay tetiklediği için burada da pazaryerine çağrı yapılmaz;
/// gönderim, hash karşılaştırmasıyla gerçekten değişeni seçen içerik senkron turuna bırakılır.
/// </remarks>
internal sealed class ProductContentChangedListingHandler(
    IMarkListingsDirtyHandler markDirty,
    ILogger<ProductContentChangedListingHandler> logger) : IIntegrationEventHandler<ProductContentChanged>
{
    /// <inheritdoc/>
    public async Task HandleAsync(
        ProductContentChanged integrationEvent,
        CancellationToken cancellationToken = default)
    {
        var marked = 0;

        foreach (var productItemId in integrationEvent.ProductItemIds)
        {
            var result = await markDirty.ExecuteAsync(
                new MarkListingsDirtyCommand(productItemId, MarketplaceCode: null, ListingDirtyKind.Content),
                cancellationToken);

            if (result.IsFailure)
            {
                // Hatayı fırlat ki OutboxProcessor mesajı işaretlemesin ve yeniden denesin.
                throw new InvalidOperationException(
                    $"İçerik değişimi listelemelere yazılamadı (ItemId={productItemId}): {result.Error.Message}");
            }

            marked += result.Value;
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "ProductContentChanged işlendi: ProductId={ProductId} için {Count} listeleme içerik-kirli işaretlendi.",
                integrationEvent.ProductId,
                marked);
        }
    }
}
