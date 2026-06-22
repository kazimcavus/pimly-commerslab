using Catalog.Application.Contracts;
using Catalog.Application.Validation;
using Catalog.Domain;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Attributes.AddAttributeValue;

/// <summary>Özelliğe yeni değer ekleme işlemini gerçekleştirir.</summary>
public sealed class AddAttributeValueHandler(
    IValidator<AddAttributeValueCommand> validator,
    IAttributeRepository attributes,
    IUnitOfWork unitOfWork) : IAddAttributeValueHandler
{
    /// <inheritdoc/>
    public async Task<Result<AttributeDefinitionValueDto>> ExecuteAsync(
        AddAttributeValueCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<AttributeDefinitionValueDto>(validationResult.Error);
        }

        var attribute = await attributes.GetByIdAsync(command.AttributeId, cancellationToken);
        if (attribute is null)
        {
            return Result.Failure<AttributeDefinitionValueDto>(Error.NotFound("Attribute not found."));
        }

        var addResult = attribute.AddValue(command.Name);
        if (addResult.IsFailure)
        {
            return Result.Failure<AttributeDefinitionValueDto>(addResult.Error);
        }

        attributes.Update(attribute);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(addResult.Value.ToDto(attribute.Id));
    }
}
