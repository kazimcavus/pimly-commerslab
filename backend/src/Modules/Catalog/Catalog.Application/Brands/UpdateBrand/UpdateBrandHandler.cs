using Catalog.Application.Contracts;
using Catalog.Application.Validation;
using Catalog.Domain;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Brands.UpdateBrand;

/// <summary>Marka güncelleme işlemini yürüten handler.</summary>
public sealed class UpdateBrandHandler(
    IValidator<UpdateBrandCommand> validator,
    IBrandRepository brands,
    IUnitOfWork unitOfWork) : IUpdateBrandHandler
{
    /// <inheritdoc/>
    public async Task<Result<BrandDto>> ExecuteAsync(
        UpdateBrandCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<BrandDto>(validationResult.Error);
        }

        var brand = await brands.GetByIdAsync(command.Id, cancellationToken);
        if (brand is null)
        {
            return Result.Failure<BrandDto>(Error.NotFound("Brand not found."));
        }

        var duplicate = await brands.GetByNameAsync(command.Name, cancellationToken);
        if (duplicate is not null && duplicate.Id != brand.Id)
        {
            return Result.Failure<BrandDto>(Error.Conflict("Brand with the same name already exists."));
        }

        var renameResult = brand.Rename(command.Name, command.Code);
        if (renameResult.IsFailure)
        {
            return Result.Failure<BrandDto>(renameResult.Error);
        }

        brands.Update(brand);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(brand.ToDto());
    }
}
