using Catalog.Application.Contracts;
using Catalog.Application.Validation;
using Catalog.Domain;
using Catalog.Domain.Brands;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Brands.CreateBrand;

/// <summary>Yeni marka oluşturma işlemini yürüten handler.</summary>
public sealed class CreateBrandHandler(
    IValidator<CreateBrandCommand> validator,
    IBrandRepository brands,
    IUnitOfWork unitOfWork) : ICreateBrandHandler
{
    /// <inheritdoc/>
    public async Task<Result<BrandDto>> ExecuteAsync(
        CreateBrandCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<BrandDto>(validationResult.Error);
        }

        if (await brands.GetByNameAsync(command.Name, cancellationToken) is not null)
        {
            return Result.Failure<BrandDto>(Error.Conflict("Brand with the same name already exists."));
        }

        var createResult = Brand.Create(command.Name, command.Code);
        if (createResult.IsFailure)
        {
            return Result.Failure<BrandDto>(createResult.Error);
        }

        await brands.AddAsync(createResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(createResult.Value.ToDto());
    }
}
