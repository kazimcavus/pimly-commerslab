using Catalog.Application.Contracts;
using Catalog.Domain;
using SharedKernel;

namespace Catalog.Application.Barcodes.GetBarcodeSequence;

/// <summary>Barkod serisi ayarını getirme işlemini yürütür.</summary>
public sealed class GetBarcodeSequenceHandler(IBarcodeSequenceRepository sequences)
    : IGetBarcodeSequenceHandler
{
    /// <inheritdoc/>
    public async Task<Result<BarcodeSequenceDto>> ExecuteAsync(
        GetBarcodeSequenceQuery query,
        CancellationToken cancellationToken = default)
    {
        var sequence = await sequences.GetAsync(cancellationToken);
        return sequence is null
            ? Result.Failure<BarcodeSequenceDto>(Error.NotFound("Barcode sequence is not configured."))
            : Result.Success(sequence.ToDto());
    }
}
