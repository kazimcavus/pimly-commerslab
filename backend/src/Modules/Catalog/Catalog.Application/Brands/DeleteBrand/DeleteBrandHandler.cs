using Catalog.Domain;
using SharedKernel;

namespace Catalog.Application.Brands.DeleteBrand;

/// <summary>Marka silme işlemini yürüten handler.</summary>
public sealed class DeleteBrandHandler(
    IBrandRepository brands,
    IUnitOfWork unitOfWork) : IDeleteBrandHandler
{
    /// <inheritdoc/>
    public async Task<Result> ExecuteAsync(
        DeleteBrandCommand command,
        CancellationToken cancellationToken = default)
    {
        var brand = await brands.GetByIdAsync(command.Id, cancellationToken);
        if (brand is null)
        {
            return Result.Failure(Error.NotFound("Brand not found."));
        }

        brands.Remove(brand);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
