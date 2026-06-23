using Catalog.Application.Contracts;
using Catalog.Application.Validation;
using Catalog.Domain;
using Catalog.Domain.Variants;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Variants.CreateVariantType;

/// <summary>Yeni varyant türü oluşturma işlemini gerçekleştirir.</summary>
public sealed class CreateVariantTypeHandler(
    IValidator<CreateVariantTypeCommand> validator,
    IVariantRepository variantTypes,
    IUnitOfWork unitOfWork) : ICreateVariantTypeHandler
{
    /// <inheritdoc/>
    public async Task<Result<VariantTypeDto>> ExecuteAsync(
        CreateVariantTypeCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<VariantTypeDto>(validationResult.Error);
        }

        if (await variantTypes.GetByNameAsync(command.Name.Trim(), cancellationToken) is not null)
        {
            return Result.Failure<VariantTypeDto>(Error.Conflict("Variant type name already exists."));
        }

        if (command.Slicer && await variantTypes.GetSlicerVariantAsync(cancellationToken: cancellationToken) is not null)
        {
            return Result.Failure<VariantTypeDto>(Error.Conflict("Only one variant type can be marked as slicer."));
        }

        var style = string.IsNullOrWhiteSpace(command.SelectionStyle)
            ? SelectionStyle.List
            : CatalogMappings.ParseSelectionStyle(command.SelectionStyle);

        var createResult = Variant.Create(command.Name, style, command.SortOrder, command.Slicer, command.Key);
        if (createResult.IsFailure)
        {
            return Result.Failure<VariantTypeDto>(createResult.Error);
        }

        if (await variantTypes.GetByKeyAsync(createResult.Value.Key.Value, cancellationToken) is not null)
        {
            return Result.Failure<VariantTypeDto>(Error.Conflict("Variant key already exists."));
        }

        await variantTypes.AddAsync(createResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(createResult.Value.ToDto());
    }
}
