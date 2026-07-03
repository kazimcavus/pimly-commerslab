using Catalog.Application.Validation;
using Catalog.Domain;
using Catalog.Domain.Products;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Products.DeleteItemChannelPrice;

/// <summary>Kalem kanal fiyatı silme işlemini yürüten handler.</summary>
public sealed class DeleteItemChannelPriceHandler(
    IValidator<DeleteItemChannelPriceCommand> validator,
    IChannelPriceRepository channelPrices,
    IUnitOfWork unitOfWork) : IDeleteItemChannelPriceHandler
{
    /// <inheritdoc/>
    public async Task<Result> ExecuteAsync(
        DeleteItemChannelPriceCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        var channelPrice = await channelPrices.GetAsync(
            command.ProductItemId,
            command.MarketplaceKey,
            cancellationToken);

        if (channelPrice is null)
        {
            return Result.Failure(Error.NotFound("Channel price not found."));
        }

        channelPrices.Remove(channelPrice);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
