using FluentValidation;
using Pricing.Application.Contracts;
using Pricing.Application.ItemPrices.Catalog;
using Pricing.Application.Validation;
using Pricing.Domain;
using Pricing.Domain.ChannelPrices;
using SharedKernel;

namespace Pricing.Application.ChannelPrices.SetChannelPrice;

/// <summary>Kalemin bir pazaryerindeki kanal fiyatını oluşturan / güncelleyen handler.</summary>
public sealed class SetChannelPriceHandler(
    IValidator<SetChannelPriceCommand> validator,
    ICatalogProductItemGateway productItems,
    IChannelPriceRepository channelPrices,
    IUnitOfWork unitOfWork) : ISetChannelPriceHandler
{
    /// <inheritdoc/>
    public async Task<Result<ChannelPriceDto>> ExecuteAsync(
        SetChannelPriceCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<ChannelPriceDto>(validationResult.Error);
        }

        var marketplaceResult = Marketplace.FromCode(command.Marketplace);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure<ChannelPriceDto>(marketplaceResult.Error);
        }

        var marketplace = marketplaceResult.Value;

        if (!await productItems.ExistsAsync(command.ProductItemId, cancellationToken))
        {
            return Result.Failure<ChannelPriceDto>(Error.NotFound("Product item not found."));
        }

        var existing = await channelPrices.GetAsync(command.ProductItemId, marketplace, cancellationToken);
        if (existing is not null)
        {
            var updateResult = existing.Update(command.Amount, command.CompareAtAmount, command.Currency);
            if (updateResult.IsFailure)
            {
                return Result.Failure<ChannelPriceDto>(updateResult.Error);
            }

            channelPrices.Update(existing);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(existing.ToDto());
        }

        var createResult = ChannelPrice.Create(
            command.ProductItemId,
            marketplace,
            command.Amount,
            command.CompareAtAmount,
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
