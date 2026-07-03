using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Products.AddProductImage;

/// <summary>Ürün galerisine görsel ekleme işlemini yürüten handler arayüzü.</summary>
public interface IAddProductImageHandler
{
    /// <summary>Komutu işler ve eklenen görseli döndürür.</summary>
    Task<Result<ProductImageDto>> ExecuteAsync(
        AddProductImageCommand command,
        CancellationToken cancellationToken = default);
}
