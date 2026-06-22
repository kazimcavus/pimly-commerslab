namespace Catalog.Domain.Products;

/// <summary>Ürün yaşam döngüsü durumları.</summary>
/// <example>Draft → Active → Archived.</example>
public enum ProductStatus
{
    Draft,
    Active,
    Archived,
}
