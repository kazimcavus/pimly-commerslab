using FluentValidation;
using Inventory.Application.Contracts;
using Inventory.Application.StockLevels.Catalog;
using Inventory.Application.Validation;
using Inventory.Domain;
using Inventory.Domain.StockLevels;
using SharedKernel;

namespace Inventory.Application.StockLevels.SetStock;

/// <summary>Kalemin stok miktarını oluşturan / güncelleyen handler.</summary>
public sealed class SetStockHandler(
    IValidator<SetStockCommand> validator,
    ICatalogProductItemGateway productItems,
    IStockLevelRepository stockLevels,
    IUnitOfWork unitOfWork) : ISetStockHandler
{
    /// <inheritdoc/>
    public async Task<Result<StockLevelDto>> ExecuteAsync(
        SetStockCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<StockLevelDto>(validationResult.Error);
        }

        if (!await productItems.ExistsAsync(command.ProductItemId, cancellationToken))
        {
            return Result.Failure<StockLevelDto>(Error.NotFound("Product item not found."));
        }

        var existing = await stockLevels.GetByItemAsync(command.ProductItemId, cancellationToken);
        if (existing is not null)
        {
            var updateResult = existing.SetQuantity(command.Quantity);
            if (updateResult.IsFailure)
            {
                return Result.Failure<StockLevelDto>(updateResult.Error);
            }

            stockLevels.Update(existing);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(existing.ToDto());
        }

        var createResult = StockLevel.Create(command.ProductItemId, command.Quantity);
        if (createResult.IsFailure)
        {
            return Result.Failure<StockLevelDto>(createResult.Error);
        }

        await stockLevels.AddAsync(createResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(createResult.Value.ToDto());
    }
}
