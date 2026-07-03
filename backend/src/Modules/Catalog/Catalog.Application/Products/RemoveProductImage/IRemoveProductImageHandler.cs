using SharedKernel;

namespace Catalog.Application.Products.RemoveProductImage;

/// <summary>Ürün galerisi görseli silme işlemini yürüten handler arayüzü.</summary>
public interface IRemoveProductImageHandler
{
    /// <summary>Komutu işler.</summary>
    Task<Result> ExecuteAsync(
        RemoveProductImageCommand command,
        CancellationToken cancellationToken = default);
}
