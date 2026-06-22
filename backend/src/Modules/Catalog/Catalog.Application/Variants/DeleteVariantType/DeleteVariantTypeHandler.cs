using Catalog.Domain;
using SharedKernel;

namespace Catalog.Application.Variants.DeleteVariantType;

/// <summary>Varyant türü silme işlemini gerçekleştirir.</summary>
public sealed class DeleteVariantTypeHandler(
    IVariantRepository variantTypes,
    IUnitOfWork unitOfWork) : IDeleteVariantTypeHandler
{
    /// <inheritdoc/>
    public async Task<Result> ExecuteAsync(
        DeleteVariantTypeCommand command,
        CancellationToken cancellationToken = default)
    {
        var variantType = await variantTypes.GetByIdAsync(command.Id, cancellationToken);
        if (variantType is null)
        {
            return Result.Failure(Error.NotFound("Variant type not found."));
        }

        variantTypes.Remove(variantType);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
