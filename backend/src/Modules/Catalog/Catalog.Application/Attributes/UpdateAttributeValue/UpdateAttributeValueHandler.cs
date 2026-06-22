using Catalog.Application.Attributes;
using Catalog.Application.Contracts;
using Catalog.Application.Validation;
using Catalog.Domain;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Attributes.UpdateAttributeValue;

/// <summary>Özellik değeri güncelleme işlemini gerçekleştirir.</summary>
public sealed class UpdateAttributeValueHandler(
    IValidator<UpdateAttributeValueCommand> validator,
    IAttributeRepository attributes,
    IUnitOfWork unitOfWork) : IUpdateAttributeValueHandler
{
    /// <inheritdoc/>
    public async Task<Result<AttributeDefinitionValueDto>> ExecuteAsync(
        UpdateAttributeValueCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<AttributeDefinitionValueDto>(validationResult.Error);
        }

        var owner = await AttributeLookup.FindByValueIdAsync(attributes, command.Id, cancellationToken);
        if (owner is null)
        {
            return Result.Failure<AttributeDefinitionValueDto>(Error.NotFound("Attribute value not found."));
        }

        var updateResult = owner.UpdateValue(command.Id, command.Name);
        if (updateResult.IsFailure)
        {
            return Result.Failure<AttributeDefinitionValueDto>(updateResult.Error);
        }

        attributes.Update(owner);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = owner.Values.First(v => v.Id == command.Id);
        return Result.Success(updated.ToDto(owner.Id));
    }
}
