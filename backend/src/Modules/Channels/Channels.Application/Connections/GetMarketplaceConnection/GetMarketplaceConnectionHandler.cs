using Channels.Application.Contracts;
using Channels.Application.Validation;
using Channels.Domain.Connections;
using FluentValidation;
using SharedKernel;

namespace Channels.Application.Connections.GetMarketplaceConnection;

/// <summary>Pazaryeri bağlantısı getirme işlemini yürüten handler.</summary>
public sealed class GetMarketplaceConnectionHandler(
    IValidator<GetMarketplaceConnectionQuery> validator,
    IMarketplaceConnectionRepository connections) : IGetMarketplaceConnectionHandler
{
    /// <inheritdoc/>
    public async Task<Result<MarketplaceConnectionDto>> ExecuteAsync(
        GetMarketplaceConnectionQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(query, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<MarketplaceConnectionDto>(validationResult.Error);
        }

        var marketplaceResult = Marketplace.FromCode(query.MarketplaceCode);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure<MarketplaceConnectionDto>(marketplaceResult.Error);
        }

        var connection = await connections.GetByMarketplaceAsync(marketplaceResult.Value, cancellationToken);
        if (connection is null)
        {
            return Result.Failure<MarketplaceConnectionDto>(Error.NotFound("Marketplace connection not found."));
        }

        return Result.Success(connection.ToDto());
    }
}
