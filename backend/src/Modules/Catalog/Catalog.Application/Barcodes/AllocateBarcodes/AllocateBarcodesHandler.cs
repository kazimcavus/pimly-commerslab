using Catalog.Application.Contracts;
using Catalog.Application.Validation;
using Catalog.Domain;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Barcodes.AllocateBarcodes;

/// <summary>Barkod tahsisi işlemini yürütür.</summary>
public sealed class AllocateBarcodesHandler(
    IValidator<AllocateBarcodesCommand> validator,
    IBarcodeAllocator barcodeAllocator,
    IUnitOfWork unitOfWork) : IAllocateBarcodesHandler
{
    /// <inheritdoc/>
    public async Task<Result<AllocateBarcodesResult>> ExecuteAsync(
        AllocateBarcodesCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<AllocateBarcodesResult>(validationResult.Error);
        }

        var allocateResult = await barcodeAllocator.AllocateAsync(command.Count, cancellationToken);
        if (allocateResult.IsFailure)
        {
            return Result.Failure<AllocateBarcodesResult>(allocateResult.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new AllocateBarcodesResult(
            allocateResult.Value.Select(allocation => allocation.Barcode).ToList()));
    }
}
