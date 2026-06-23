using Catalog.Domain.Barcodes;
using SharedKernel;

namespace Catalog.Domain;

/// <summary>Barkod tahsis kayıtlarının kalıcılık işlemlerini tanımlar.</summary>
public interface IBarcodeAllocationRepository
{
    Task<bool> ExistsAsync(string barcode, CancellationToken cancellationToken = default);

    Task<long> MaxNumericBarcodeAsync(CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IEnumerable<BarcodeAllocation> allocations,
        CancellationToken cancellationToken = default);

    Task<PagedResult<BarcodeAllocation>> ListAsync(
        Pagination pagination,
        CancellationToken cancellationToken = default);
}
