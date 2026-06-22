using Catalog.Domain;
using SharedKernel;

namespace Catalog.Application.Variants.RemoveVariantValue;

/// <summary>Varyant değeri silme işlemini gerçekleştirir.</summary>
public sealed class RemoveVariantValueHandler(
    IVariantRepository variantTypes,
    IUnitOfWork unitOfWork) : IRemoveVariantValueHandler
{
    /// <inheritdoc/>
    public async Task<Result> ExecuteAsync(
        RemoveVariantValueCommand command,
        CancellationToken cancellationToken = default)
    {
        var owner = await VariantTypeLookup.FindByValueIdAsync(variantTypes, command.Id, cancellationToken);
        if (owner is null)
        {
            return Result.Failure(Error.NotFound("Variant value not found."));
        }

        var removeResult = owner.RemoveValue(command.Id);
        if (removeResult.IsFailure)
        {
            return removeResult;
        }

        variantTypes.Update(owner);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
