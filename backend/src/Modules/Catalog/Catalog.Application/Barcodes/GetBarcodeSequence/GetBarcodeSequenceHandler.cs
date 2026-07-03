using Catalog.Application.Contracts;
using Catalog.Domain;
using Catalog.Domain.Barcodes;
using SharedKernel;

namespace Catalog.Application.Barcodes.GetBarcodeSequence;

/// <summary>Barkod serisi ayarını getirme işlemini yürütür.</summary>
public sealed class GetBarcodeSequenceHandler(
    IBarcodeSequenceRepository sequences,
    IUnitOfWork unitOfWork) : IGetBarcodeSequenceHandler
{
    /// <inheritdoc/>
    public async Task<Result<BarcodeSequenceDto>> ExecuteAsync(
        GetBarcodeSequenceQuery query,
        CancellationToken cancellationToken = default)
    {
        _ = query;

        var sequence = await sequences.GetAsync(cancellationToken);
        if (sequence is null)
        {
            sequence = BarcodeSequence.CreateInitial();
            await sequences.AddAsync(sequence, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(sequence.ToDto());
    }
}
