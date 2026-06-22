using Catalog.Application.Contracts;
using Catalog.Application.Validation;
using Catalog.Domain;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Variants.UpdateVariantValue;

/// <summary>Varyant değeri güncelleme işlemini gerçekleştirir.</summary>
public sealed class UpdateVariantValueHandler(
    IValidator<UpdateVariantValueCommand> validator,
    IVariantRepository variantTypes,
    IUnitOfWork unitOfWork) : IUpdateVariantValueHandler
{
    /// <inheritdoc/>
    public async Task<Result<VariantValueDto>> ExecuteAsync(
        UpdateVariantValueCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<VariantValueDto>(validationResult.Error);
        }

        var owner = await VariantTypeLookup.FindByValueIdAsync(variantTypes, command.Id, cancellationToken);
        if (owner is null)
        {
            return Result.Failure<VariantValueDto>(Error.NotFound("Variant value not found."));
        }

        var updateResult = owner.UpdateValue(
            command.Id,
            command.Label,
            command.Color,
            command.ImageUrl,
            command.Code,
            command.SortOrder);

        if (updateResult.IsFailure)
        {
            return Result.Failure<VariantValueDto>(updateResult.Error);
        }

        variantTypes.Update(owner);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = owner.Values.First(v => v.Id == command.Id);
        return Result.Success(updated.ToDto(owner.Id));
    }
}
