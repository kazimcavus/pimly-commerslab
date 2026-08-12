using Catalog.Domain.Products.Events;
using Pricing.Application.ItemPrices.DeleteItemPricesForItem;
using SharedKernel;

namespace Pimly.Outbox.Worker;

/// <summary>
/// <see cref="ProductItemDeleted"/> olayını dinleyip Pricing'deki kalem fiyatlarını temizleyen adapter.
/// Worker kompozisyon kökü hem Catalog olay sözleşmesini hem Pricing use-case'ini bildiği için köprü
/// burada kurulur; Pricing.Application, Catalog.Domain'e bağımlı kalmaz.
/// </summary>
internal sealed class ProductItemDeletedPricingHandler(
    IDeleteItemPricesForItemHandler deleteItemPrices,
    ILogger<ProductItemDeletedPricingHandler> logger) : IIntegrationEventHandler<ProductItemDeleted>
{
    /// <inheritdoc/>
    public async Task HandleAsync(ProductItemDeleted integrationEvent, CancellationToken cancellationToken = default)
    {
        var result = await deleteItemPrices.ExecuteAsync(
            new DeleteItemPricesForItemCommand(integrationEvent.ProductItemId),
            cancellationToken);

        if (result.IsFailure)
        {
            // Hatayı fırlat ki OutboxProcessor mesajı işaretlemesin ve yeniden denesin.
            throw new InvalidOperationException(
                $"Kalem fiyatları silinemedi (ItemId={integrationEvent.ProductItemId}): {result.Error.Message}");
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "ProductItemDeleted işlendi: ItemId={ItemId} için Pricing fiyatları temizlendi.",
                integrationEvent.ProductItemId);
        }
    }
}
