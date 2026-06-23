using Catalog.Application.Contracts;
using Catalog.Application.Validation;
using Catalog.Domain;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Variants.AddVariantValue;

/// <summary>Varyant türüne yeni değer ekleme işlemini gerçekleştirir.</summary>
public sealed class AddVariantValueHandler(
    IValidator<AddVariantValueCommand> validator,
    IVariantRepository variantTypes,
    IUnitOfWork unitOfWork) : IAddVariantValueHandler
{
    /// <inheritdoc/>
    public async Task<Result<VariantValueDto>> ExecuteAsync(
        AddVariantValueCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<VariantValueDto>(validationResult.Error);
        }

        var variantType = await variantTypes.GetByIdAsync(command.VariantTypeId, cancellationToken);
        if (variantType is null)
        {
            return Result.Failure<VariantValueDto>(Error.NotFound("Variant type not found."));
        }

        var addResult = variantType.AddValue(
            command.Label,
            command.Color,
            command.ImageUrl,
            command.Key,
            command.SortOrder);

        if (addResult.IsFailure)
        {
            return Result.Failure<VariantValueDto>(addResult.Error);
        }

        variantTypes.Update(variantType);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(addResult.Value.ToDto(variantType.Id));
    }
}
