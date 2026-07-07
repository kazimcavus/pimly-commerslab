using Catalog.Application.Contracts;
using Catalog.Application.Validation;
using Catalog.Domain;
using Catalog.Domain.Products;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Products.UpsertItemPrice;

/// <summary>Kalem fiyatı oluşturma / güncelleme işlemini yürüten handler.</summary>
public sealed class UpsertItemPriceHandler(
    IValidator<UpsertItemPriceCommand> validator,
    IProductRepository products,
    IPriceDefinitionRepository priceDefinitions,
    IItemPriceRepository itemPrices,
    IUnitOfWork unitOfWork) : IUpsertItemPriceHandler
{
    /// <inheritdoc/>
    public async Task<Result<ItemPriceDto>> ExecuteAsync(
        UpsertItemPriceCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<ItemPriceDto>(validationResult.Error);
        }

        var item = await products.GetItemByIdAsync(command.ProductItemId, cancellationToken);
        if (item is null)
        {
            return Result.Failure<ItemPriceDto>(Error.NotFound("Product item not found."));
        }

        var definition = await priceDefinitions.GetByIdAsync(command.PriceDefinitionId, cancellationToken);
        if (definition is null)
        {
            return Result.Failure<ItemPriceDto>(Error.NotFound("Price definition not found."));
        }

        var existing = await itemPrices.GetAsync(command.ProductItemId, command.PriceDefinitionId, cancellationToken);
        if (existing is not null)
        {
            var updateResult = existing.UpdateAmount(command.Amount, command.Currency);
            if (updateResult.IsFailure)
            {
                return Result.Failure<ItemPriceDto>(updateResult.Error);
            }

            itemPrices.Update(existing);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(existing.ToDto(definition.Name));
        }

        var createResult = ProductItemPrice.Create(
            command.ProductItemId,
            command.PriceDefinitionId,
            command.Amount,
            command.Currency);

        if (createResult.IsFailure)
        {
            return Result.Failure<ItemPriceDto>(createResult.Error);
        }

        await itemPrices.AddAsync(createResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(createResult.Value.ToDto(definition.Name));
    }
}
