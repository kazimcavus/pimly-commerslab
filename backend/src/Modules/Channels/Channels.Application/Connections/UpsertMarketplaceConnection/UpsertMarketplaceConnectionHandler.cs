using Channels.Application.Contracts;
using Channels.Application.Validation;
using Channels.Domain;
using Channels.Domain.Connections;
using FluentValidation;
using SharedKernel;
using SharedKernel.Tenancy;

namespace Channels.Application.Connections.UpsertMarketplaceConnection;

/// <summary>Pazaryeri bağlantısı upsert işlemini yürüten handler.</summary>
public sealed class UpsertMarketplaceConnectionHandler(
    IValidator<UpsertMarketplaceConnectionCommand> validator,
    IMarketplaceConnectionRepository connections,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork) : IUpsertMarketplaceConnectionHandler
{
    /// <inheritdoc/>
    public async Task<Result<MarketplaceConnectionDto>> ExecuteAsync(
        UpsertMarketplaceConnectionCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<MarketplaceConnectionDto>(validationResult.Error);
        }

        var marketplaceResult = Marketplace.FromCode(command.MarketplaceCode);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure<MarketplaceConnectionDto>(marketplaceResult.Error);
        }

        var marketplace = marketplaceResult.Value;

        var existing = await connections.GetByMarketplaceAsync(marketplace, cancellationToken);

        if (existing is null)
        {
            var createResult = MarketplaceConnection.Create(
                tenantContext.TenantId,
                marketplace,
                command.SellerId,
                command.ApiKey,
                command.ApiSecret,
                command.IsEnabled);

            if (createResult.IsFailure)
            {
                return Result.Failure<MarketplaceConnectionDto>(createResult.Error);
            }

            await connections.AddAsync(createResult.Value, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(createResult.Value.ToDto());
        }

        var updateResult = existing.Update(
            command.SellerId,
            command.ApiKey,
            command.ApiSecret,
            command.IsEnabled);

        if (updateResult.IsFailure)
        {
            return Result.Failure<MarketplaceConnectionDto>(updateResult.Error);
        }

        connections.Update(existing);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(existing.ToDto());
    }
}
