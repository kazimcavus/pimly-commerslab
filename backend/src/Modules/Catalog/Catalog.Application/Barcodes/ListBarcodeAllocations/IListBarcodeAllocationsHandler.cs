using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Barcodes.ListBarcodeAllocations;

/// <summary>Barkod tahsis kayıtlarını listeleme işlemini yürütür.</summary>
public interface IListBarcodeAllocationsHandler
{
    Task<Result<PagedResult<BarcodeAllocationDto>>> ExecuteAsync(
        ListBarcodeAllocationsQuery query,
        CancellationToken cancellationToken = default);
}
