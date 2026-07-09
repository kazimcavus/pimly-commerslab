using Pricing.Application.Contracts;
using Pricing.Domain.BasePrices;
using SharedKernel;

namespace Pricing.Application.BasePrices.GetBasePrice;

/// <summary>Kalemin temel fiyatını getiren handler.</summary>
public sealed class GetBasePriceHandler(IBasePriceRepository basePrices) : IGetBasePriceHandler
{
    /// <inheritdoc/>
    public async Task<Result<BasePriceDto>> ExecuteAsync(
        GetBasePriceQuery query,
        CancellationToken cancellationToken = default)
    {
        var basePrice = await basePrices.GetByItemAsync(query.ProductItemId, cancellationToken);
        return basePrice is null
            ? Result.Failure<BasePriceDto>(Error.NotFound("Base price not found."))
            : Result.Success(basePrice.ToDto());
    }
}
