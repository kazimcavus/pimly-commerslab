using Catalog.Application.Contracts;
using Catalog.Application.Validation;
using Catalog.Domain;
using Catalog.Domain.Variants;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Variants.UpdateVariantType;

/// <summary>Varyant türü güncelleme işlemini gerçekleştirir.</summary>
public sealed class UpdateVariantTypeHandler(
    IValidator<UpdateVariantTypeCommand> validator,
    IVariantRepository variantTypes,
    IUnitOfWork unitOfWork) : IUpdateVariantTypeHandler
{
    /// <inheritdoc/>
    public async Task<Result<VariantTypeDto>> ExecuteAsync(
        UpdateVariantTypeCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<VariantTypeDto>(validationResult.Error);
        }

        var variantType = await variantTypes.GetByIdAsync(command.Id, cancellationToken);
        if (variantType is null)
        {
            return Result.Failure<VariantTypeDto>(Error.NotFound("Variant type not found."));
        }

        var existing = await variantTypes.GetByNameAsync(command.Name.Trim(), cancellationToken);
        if (existing is not null && existing.Id != command.Id)
        {
            return Result.Failure<VariantTypeDto>(Error.Conflict("Variant type name already exists."));
        }

        var style = string.IsNullOrWhiteSpace(command.SelectionStyle)
            ? SelectionStyle.List
            : CatalogMappings.ParseSelectionStyle(command.SelectionStyle);

        var updateResult = variantType.Rename(command.Name, style, command.SortOrder, command.Slicer);
        if (updateResult.IsFailure)
        {
            return Result.Failure<VariantTypeDto>(updateResult.Error);
        }

        variantTypes.Update(variantType);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(variantType.ToDto());
    }
}
