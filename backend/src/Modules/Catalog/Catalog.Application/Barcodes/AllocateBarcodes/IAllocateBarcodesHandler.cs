using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Barcodes.AllocateBarcodes;

/// <summary>Barkod tahsisi işlemini yürütür.</summary>
public interface IAllocateBarcodesHandler
{
    Task<Result<AllocateBarcodesResult>> ExecuteAsync(
        AllocateBarcodesCommand command,
        CancellationToken cancellationToken = default);
}
