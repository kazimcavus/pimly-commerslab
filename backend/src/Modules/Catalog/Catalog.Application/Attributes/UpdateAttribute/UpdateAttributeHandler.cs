using Catalog.Application.Contracts;
using Catalog.Application.Validation;
using Catalog.Domain;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Attributes.UpdateAttribute;

/// <summary>Öznitelik güncelleme işlemini gerçekleştirir.</summary>
public sealed class UpdateAttributeHandler(
    IValidator<UpdateAttributeCommand> validator,
    IAttributeRepository attributes,
    IUnitOfWork unitOfWork) : IUpdateAttributeHandler
{
    /// <inheritdoc/>
    public async Task<Result<AttributeDto>> ExecuteAsync(
        UpdateAttributeCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<AttributeDto>(validationResult.Error);
        }

        var attribute = await attributes.GetByIdAsync(command.Id, cancellationToken);
        if (attribute is null)
        {
            return Result.Failure<AttributeDto>(Error.NotFound("Attribute not found."));
        }

        var updateResult = attribute.Rename(command.Name);
        if (updateResult.IsFailure)
        {
            return Result.Failure<AttributeDto>(updateResult.Error);
        }

        attributes.Update(attribute);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(attribute.ToDto());
    }
}
