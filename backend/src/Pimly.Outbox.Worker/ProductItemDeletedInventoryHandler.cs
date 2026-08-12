using Catalog.Domain.Products.Events;
using Inventory.Application.StockLevels.DeleteStockForItem;
using SharedKernel;

namespace Pimly.Outbox.Worker;

/// <summary>
/// <see cref="ProductItemDeleted"/> olayını dinleyip Inventory'deki stok kaydını temizleyen adapter.
/// Pricing adapter'ının stok karşılığıdır; Inventory.Application, Catalog.Domain'e bağımlı kalmaz.
/// </summary>
internal sealed class ProductItemDeletedInventoryHandler(
    IDeleteStockForItemHandler deleteStock,
    ILogger<ProductItemDeletedInventoryHandler> logger) : IIntegrationEventHandler<ProductItemDeleted>
{
    /// <inheritdoc/>
    public async Task HandleAsync(ProductItemDeleted integrationEvent, CancellationToken cancellationToken = default)
    {
        var result = await deleteStock.ExecuteAsync(
            new DeleteStockForItemCommand(integrationEvent.ProductItemId),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Stok silinemedi (ItemId={integrationEvent.ProductItemId}): {result.Error.Message}");
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "ProductItemDeleted işlendi: ItemId={ItemId} için Inventory stoğu temizlendi.",
                integrationEvent.ProductItemId);
        }
    }
}
