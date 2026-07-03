using Catalog.Application.Contracts;
using Catalog.Application.Validation;
using Catalog.Domain;
using Catalog.Domain.Products;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Products.UpsertItemChannelPrice;

/// <summary>Kalem kanal fiyatı oluşturma / güncelleme işlemini yürüten handler.</summary>
public sealed class UpsertItemChannelPriceHandler(
    IValidator<UpsertItemChannelPriceCommand> validator,
    IProductRepository products,
    IChannelPriceRepository channelPrices,
    IUnitOfWork unitOfWork) : IUpsertItemChannelPriceHandler
{
    /// <inheritdoc/>
    public async Task<Result<ChannelPriceDto>> ExecuteAsync(
        UpsertItemChannelPriceCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<ChannelPriceDto>(validationResult.Error);
        }

        var item = await products.GetItemByIdAsync(command.ProductItemId, cancellationToken);
        if (item is null)
        {
            return Result.Failure<ChannelPriceDto>(Error.NotFound("Product item not found."));
        }

        var existing = await channelPrices.GetAsync(command.ProductItemId, command.MarketplaceKey, cancellationToken);
        if (existing is not null)
        {
            var updateResult = existing.UpdatePrice(command.Price, command.CompareAtPrice, command.Currency);
            if (updateResult.IsFailure)
            {
                return Result.Failure<ChannelPriceDto>(updateResult.Error);
            }

            channelPrices.Update(existing);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(existing.ToDto());
        }

        var createResult = ProductItemChannelPrice.Create(
            command.ProductItemId,
            command.MarketplaceKey,
            command.Price,
            command.CompareAtPrice,
            command.Currency);

        if (createResult.IsFailure)
        {
            return Result.Failure<ChannelPriceDto>(createResult.Error);
        }

        await channelPrices.AddAsync(createResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(createResult.Value.ToDto());
    }
}
