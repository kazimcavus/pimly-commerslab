using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Products.AddProductItem;

/// <summary>Mevcut ürüne kalem ekleme işlemini yürüten handler sözleşmesi.</summary>
public interface IAddProductItemHandler
{
    /// <summary>Komutu çalıştırır ve eklenen kalemi döndürür.</summary>
    /// <param name="command">Kalem ekleme komutu.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<Result<ProductItemDto>> ExecuteAsync(
        AddProductItemCommand command,
        CancellationToken cancellationToken = default);
}
