using Catalog.Domain.Products.Events;
using SharedKernel;

namespace Pimly.Catalog.Worker;

/// <summary>
/// İskelet kanıtı: <see cref="ProductItemCreated"/> olayını loglar. Boru uçtan uca çalıştığını
/// gösterir; Faz 1'de yerini gerçek "default ItemPrice oluştur" handler'ı (Pricing) alır.
/// </summary>
internal sealed class ProductItemCreatedLoggingHandler(ILogger<ProductItemCreatedLoggingHandler> logger)
    : IIntegrationEventHandler<ProductItemCreated>
{
    /// <inheritdoc/>
    public Task HandleAsync(ProductItemCreated integrationEvent, CancellationToken cancellationToken = default)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Integration event alındı: ProductItemCreated ItemId={ItemId} ProductId={ProductId}",
                integrationEvent.ProductItemId,
                integrationEvent.ProductId);
        }

        return Task.CompletedTask;
    }
}
