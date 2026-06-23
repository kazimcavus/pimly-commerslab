using Catalog.Application.Contracts;
using Catalog.Domain;
using SharedKernel;

namespace Catalog.Application.Barcodes.ListBarcodeAllocations;

/// <summary>Barkod tahsis kayıtlarını listeleme işlemini yürütür.</summary>
public sealed class ListBarcodeAllocationsHandler(IBarcodeAllocationRepository allocations)
    : IListBarcodeAllocationsHandler
{
    /// <inheritdoc/>
    public async Task<Result<PagedResult<BarcodeAllocationDto>>> ExecuteAsync(
        ListBarcodeAllocationsQuery query,
        CancellationToken cancellationToken = default)
    {
        var paginationResult = PaginationSupport.Resolve(query.Page, query.PageSize);
        if (paginationResult.IsFailure)
        {
            return Result.Failure<PagedResult<BarcodeAllocationDto>>(paginationResult.Error);
        }

        var page = await allocations.ListAsync(paginationResult.Value, cancellationToken);
        return Result.Success(page.Map(allocation => allocation.ToDto()));
    }
}
