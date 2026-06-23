using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Barcodes.GetBarcodeSequence;

/// <summary>Barkod serisi ayarını getirme işlemini yürütür.</summary>
public interface IGetBarcodeSequenceHandler
{
    Task<Result<BarcodeSequenceDto>> ExecuteAsync(
        GetBarcodeSequenceQuery query,
        CancellationToken cancellationToken = default);
}
