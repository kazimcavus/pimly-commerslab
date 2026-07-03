using Catalog.Application.Contracts;
using Catalog.Application.Validation;
using Catalog.Domain;
using Catalog.Domain.Barcodes;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Barcodes.UpdateBarcodeSequence;

/// <summary>Barkod serisi ayarını güncelleme işlemini yürütür.</summary>
public sealed class UpdateBarcodeSequenceHandler(
    IValidator<UpdateBarcodeSequenceCommand> validator,
    IBarcodeSequenceRepository sequences,
    IBarcodeAllocationRepository allocations,
    IUnitOfWork unitOfWork) : IUpdateBarcodeSequenceHandler
{
    /// <inheritdoc/>
    public async Task<Result<BarcodeSequenceDto>> ExecuteAsync(
        UpdateBarcodeSequenceCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<BarcodeSequenceDto>(validationResult.Error);
        }

        var sequence = await sequences.GetAsync(cancellationToken);
        if (sequence is null)
        {
            sequence = BarcodeSequence.CreateInitial();
            await sequences.AddAsync(sequence, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var maxAllocated = await allocations.MaxNumericBarcodeAsync(cancellationToken);
        var updateResult = sequence.UpdateSettings(
            command.NextValue,
            command.ClientAllocationRequired,
            maxAllocated);

        if (updateResult.IsFailure)
        {
            return Result.Failure<BarcodeSequenceDto>(updateResult.Error);
        }

        sequences.Update(sequence);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(sequence.ToDto());
    }
}
