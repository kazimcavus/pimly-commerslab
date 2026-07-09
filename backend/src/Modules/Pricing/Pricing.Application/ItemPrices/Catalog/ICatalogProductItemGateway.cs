namespace Pricing.Application.ItemPrices.Catalog;

/// <summary>
/// Catalog modülünden satılabilir kalemin varlığını doğrulayan ACL portu.
/// Pricing, kaleme opak (product_item_id) referans tutar; fiyat yazmadan önce
/// kalemin gerçekten var olduğunu bu port üzerinden teyit eder.
/// </summary>
public interface ICatalogProductItemGateway
{
    /// <summary>Belirtilen kalemin (bu tenant'ta) var olup olmadığını döner.</summary>
    /// <param name="productItemId">Kalem tanımlayıcısı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<bool> ExistsAsync(Guid productItemId, CancellationToken cancellationToken = default);
}
