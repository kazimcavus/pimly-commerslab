using Catalog.Application.Contracts;
using Catalog.Domain;
using SharedKernel;

namespace Catalog.Application.Brands.GetBrand;

/// <summary>Marka getirme işlemini yürüten handler.</summary>
public sealed class GetBrandHandler(IBrandRepository brands) : IGetBrandHandler
{
    /// <inheritdoc/>
    public async Task<Result<BrandDto>> ExecuteAsync(
        GetBrandQuery query,
        CancellationToken cancellationToken = default)
    {
        var brand = await brands.GetByIdAsync(query.Id, cancellationToken);
        return brand is null
            ? Result.Failure<BrandDto>(Error.NotFound("Brand not found."))
            : Result.Success(brand.ToDto());
    }
}
