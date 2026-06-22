using Catalog.Domain;
using SharedKernel;

namespace Catalog.Application.Attributes.DeleteAttribute;

/// <summary>Öznitelik silme işlemini gerçekleştirir.</summary>
public sealed class DeleteAttributeHandler(
    IAttributeRepository attributes,
    IUnitOfWork unitOfWork) : IDeleteAttributeHandler
{
    /// <inheritdoc/>
    public async Task<Result> ExecuteAsync(
        DeleteAttributeCommand command,
        CancellationToken cancellationToken = default)
    {
        var attribute = await attributes.GetByIdAsync(command.Id, cancellationToken);
        if (attribute is null)
        {
            return Result.Failure(Error.NotFound("Attribute not found."));
        }

        attributes.Remove(attribute);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
