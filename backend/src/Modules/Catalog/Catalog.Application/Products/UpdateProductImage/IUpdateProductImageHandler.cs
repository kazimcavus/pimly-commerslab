using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Products.UpdateProductImage;

/// <summary>Ürün galerisi görseli güncelleme işlemini yürüten handler arayüzü.</summary>
public interface IUpdateProductImageHandler
{
    /// <summary>Komutu işler ve güncellenen görseli döndürür.</summary>
    Task<Result<ProductImageDto>> ExecuteAsync(
        UpdateProductImageCommand command,
        CancellationToken cancellationToken = default);
}
