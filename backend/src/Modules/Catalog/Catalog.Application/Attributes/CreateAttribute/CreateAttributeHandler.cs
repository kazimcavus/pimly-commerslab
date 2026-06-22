using Catalog.Application.Contracts;
using Catalog.Application.Validation;
using Catalog.Domain;
using Catalog.Domain.Attributes;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Attributes.CreateAttribute;

/// <summary>Yeni öznitelik oluşturma işlemini gerçekleştirir.</summary>
public sealed class CreateAttributeHandler(
    IValidator<CreateAttributeCommand> validator,
    IAttributeRepository attributes,
    IUnitOfWork unitOfWork) : ICreateAttributeHandler
{
    /// <inheritdoc/>
    public async Task<Result<AttributeDto>> ExecuteAsync(
        CreateAttributeCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<AttributeDto>(validationResult.Error);
        }

        var createResult = Domain.Attributes.Attribute.Create(command.Name);
        if (createResult.IsFailure)
        {
            return Result.Failure<AttributeDto>(createResult.Error);
        }

        if (await attributes.GetByKeyAsync(createResult.Value.Key.Value, cancellationToken) is not null)
        {
            return Result.Failure<AttributeDto>(Error.Conflict("Attribute key already exists."));
        }

        await attributes.AddAsync(createResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(createResult.Value.ToDto());
    }
}
