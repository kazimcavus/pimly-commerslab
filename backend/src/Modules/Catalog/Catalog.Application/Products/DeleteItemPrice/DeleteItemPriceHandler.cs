using Catalog.Application.Validation;
using Catalog.Domain;
using Catalog.Domain.Products;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Products.DeleteItemPrice;

/// <summary>Kalem fiyatı silme işlemini yürüten handler.</summary>
public sealed class DeleteItemPriceHandler(
    IValidator<DeleteItemPriceCommand> validator,
    IItemPriceRepository itemPrices,
    IUnitOfWork unitOfWork) : IDeleteItemPriceHandler
{
    /// <inheritdoc/>
    public async Task<Result> ExecuteAsync(
        DeleteItemPriceCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        var itemPrice = await itemPrices.GetAsync(
            command.ProductItemId,
            command.PriceDefinitionId,
            cancellationToken);

        if (itemPrice is null)
        {
            return Result.Failure(Error.NotFound("Item price not found."));
        }

        itemPrices.Remove(itemPrice);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
