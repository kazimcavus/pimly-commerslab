using SharedKernel;

namespace Catalog.Application.SkuGenerator;

/// <summary>SKU counter segmenti için atomik sayaç rezervasyonu.</summary>
public interface ISkuCounterAllocator
{
    Task<Result<long>> ReserveAsync(int count, CancellationToken cancellationToken = default);
}
