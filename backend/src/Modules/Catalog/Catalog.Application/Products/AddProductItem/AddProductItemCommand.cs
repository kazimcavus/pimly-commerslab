using Catalog.Application.Products.CreateProduct;

namespace Catalog.Application.Products.AddProductItem;

/// <summary>Mevcut ürüne yeni satılabilir kalem ekleme komutu.</summary>
/// <example>Vizon halıya "80 x 150" ölçüsünü yeni barkod/fiyat/stok ile eklemek.</example>
public sealed record AddProductItemCommand(
    Guid ProductId,
    CreateProductItemInput Item);
