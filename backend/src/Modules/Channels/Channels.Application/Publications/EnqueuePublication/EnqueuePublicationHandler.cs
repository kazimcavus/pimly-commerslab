using Channels.Application.Contracts;
using Channels.Application.Validation;
using Channels.Domain;
using Channels.Domain.Connections;
using Channels.Domain.Publications;
using FluentValidation;
using SharedKernel;
using SharedKernel.Tenancy;

namespace Channels.Application.Publications.EnqueuePublication;

/// <summary>
/// Tenant'ın ürünlerini bir pazaryerine yayımlamak (publish) için yeni bir job kuyruğa alır.
/// EnqueueProductImport'un outbound aynasıdır.
/// </summary>
/// <remarks>
/// <para><b>Ön koşullar:</b> Pazaryeri kayıtlı; tenant'ın etkin bağlantısı (SellerId + ApiSecret) olmalı;
/// aynı pazaryerinde aktif başka yayın job'ı olmamalı.</para>
/// <para><b>API:</b> POST /marketplaces/{key}/publications endpoint'i üzerinden kullanılır.</para>
/// </remarks>
public sealed class EnqueuePublicationHandler(
    IValidator<EnqueuePublicationCommand> validator,
    IMarketplaceConnectionRepository connections,
    IProductPublicationRunRepository publicationRuns,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IEnqueuePublicationHandler
{
    /// <inheritdoc/>
    public async Task<Result<ProductPublicationRunDto>> ExecuteAsync(
        EnqueuePublicationCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<ProductPublicationRunDto>(validationResult.Error);
        }

        var marketplaceResult = Marketplace.FromCode(command.MarketplaceCode);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure<ProductPublicationRunDto>(marketplaceResult.Error);
        }

        var marketplace = marketplaceResult.Value;

        var connection = await connections.GetByMarketplaceAsync(marketplace, cancellationToken);
        if (connection is null)
        {
            return Result.Failure<ProductPublicationRunDto>(
                Error.NotFound("Marketplace connection is required before publishing products."));
        }

        if (!connection.IsEnabled)
        {
            return Result.Failure<ProductPublicationRunDto>(
                Error.Validation("Marketplace connection is disabled."));
        }

        if (string.IsNullOrWhiteSpace(connection.SellerId) || string.IsNullOrWhiteSpace(connection.ApiSecret))
        {
            return Result.Failure<ProductPublicationRunDto>(
                Error.Validation("Marketplace connection requires seller id and api secret for publishing."));
        }

        var activeRun = await publicationRuns.GetActiveForTenantAsync(
            tenantContext.TenantId,
            marketplace,
            cancellationToken);

        if (activeRun is not null)
        {
            return Result.Failure<ProductPublicationRunDto>(
                Error.Conflict("A publication is already pending or running for this marketplace."));
        }

        var createResult = ProductPublicationRun.Create(
            tenantContext.TenantId,
            marketplace,
            timeProvider.GetUtcNow());

        if (createResult.IsFailure)
        {
            return Result.Failure<ProductPublicationRunDto>(createResult.Error);
        }

        await publicationRuns.AddAsync(createResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(createResult.Value.ToDto());
    }
}
