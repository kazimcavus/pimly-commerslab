namespace Inventory.Application.StockLevels.Catalog;

/// <summary>
/// Catalog modülünden satılabilir kalemin varlığını doğrulayan ACL portu. Inventory kaleme opak
/// (product_item_id) referans tutar; stok yazmadan önce kalemin var olduğunu bu port üzerinden teyit eder.
/// </summary>
public interface ICatalogProductItemGateway
{
    /// <summary>Belirtilen kalemin (bu tenant'ta) var olup olmadığını döner.</summary>
    Task<bool> ExistsAsync(Guid productItemId, CancellationToken cancellationToken = default);
}
