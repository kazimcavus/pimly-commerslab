using Catalog.Application.Contracts;
using Catalog.Domain;
using SharedKernel;

namespace Catalog.Application.Variants.GetVariantType;

/// <summary>Tek bir varyant türünü kimliğine göre getirir.</summary>
public sealed class GetVariantTypeHandler(IVariantRepository variantTypes) : IGetVariantTypeHandler
{
    /// <inheritdoc/>
    public async Task<Result<VariantTypeDto>> ExecuteAsync(
        GetVariantTypeQuery query,
        CancellationToken cancellationToken = default)
    {
        var variantType = await variantTypes.GetByIdAsync(query.Id, cancellationToken);
        return variantType is null
            ? Result.Failure<VariantTypeDto>(Error.NotFound("Variant type not found."))
            : Result.Success(variantType.ToDto());
    }
}
