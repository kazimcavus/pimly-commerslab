using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Barcodes.UpdateBarcodeSequence;

/// <summary>Barkod serisi ayarını güncelleme işlemini yürütür.</summary>
public interface IUpdateBarcodeSequenceHandler
{
    Task<Result<BarcodeSequenceDto>> ExecuteAsync(
        UpdateBarcodeSequenceCommand command,
        CancellationToken cancellationToken = default);
}
