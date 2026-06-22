using Catalog.Application.Attributes;
using Catalog.Domain;
using SharedKernel;

namespace Catalog.Application.Attributes.RemoveAttributeValue;

/// <summary>Özellik değeri silme işlemini gerçekleştirir.</summary>
public sealed class RemoveAttributeValueHandler(
    IAttributeRepository attributes,
    IUnitOfWork unitOfWork) : IRemoveAttributeValueHandler
{
    /// <inheritdoc/>
    public async Task<Result> ExecuteAsync(
        RemoveAttributeValueCommand command,
        CancellationToken cancellationToken = default)
    {
        var owner = await AttributeLookup.FindByValueIdAsync(attributes, command.Id, cancellationToken);
        if (owner is null)
        {
            return Result.Failure(Error.NotFound("Attribute value not found."));
        }

        var removeResult = owner.RemoveValue(command.Id);
        if (removeResult.IsFailure)
        {
            return removeResult;
        }

        attributes.Update(owner);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
