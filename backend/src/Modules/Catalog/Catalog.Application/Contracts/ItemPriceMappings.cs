using Catalog.Domain.Products;

namespace Catalog.Application.Contracts;

/// <summary>ProductItemPrice domain modeli ile DTO arasında dönüşüm sağlar.</summary>
internal static class ItemPriceMappings
{
    public static ItemPriceDto ToDto(this ProductItemPrice itemPrice, string definitionName) =>
        new(
            itemPrice.Id,
            itemPrice.ProductItemId,
            itemPrice.PriceDefinitionId,
            definitionName,
            itemPrice.Amount,
            itemPrice.Currency,
            itemPrice.UpdatedAt);
}
