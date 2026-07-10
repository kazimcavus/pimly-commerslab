using Channels.Application.Contracts;
using Channels.Application.Validation;
using Channels.Domain.Publications;
using FluentValidation;
using SharedKernel;
using SharedKernel.Tenancy;

namespace Channels.Application.Publications.GetPublicationRun;

/// <summary>Tenant'a ait tek bir ürün yayın run'ının ayrıntılı durumunu getirir.</summary>
public sealed class GetPublicationRunHandler(
    IValidator<GetPublicationRunQuery> validator,
    IProductPublicationRunRepository publicationRuns,
    ITenantContext tenantContext) : IGetPublicationRunHandler
{
    /// <inheritdoc/>
    public async Task<Result<ProductPublicationRunDto>> ExecuteAsync(
        GetPublicationRunQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(query, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<ProductPublicationRunDto>(validationResult.Error);
        }

        var marketplaceResult = Marketplace.FromCode(query.MarketplaceCode);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure<ProductPublicationRunDto>(marketplaceResult.Error);
        }

        var run = await publicationRuns.GetByIdAsync(query.RunId, cancellationToken);
        if (run is null
            || run.TenantId != tenantContext.TenantId
            || run.Marketplace != marketplaceResult.Value)
        {
            return Result.Failure<ProductPublicationRunDto>(Error.NotFound("Publication run not found."));
        }

        return Result.Success(run.ToDto());
    }
}
