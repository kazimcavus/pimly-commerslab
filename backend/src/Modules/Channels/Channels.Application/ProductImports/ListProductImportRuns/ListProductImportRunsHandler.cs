using Channels.Application.Contracts;
using Channels.Application.Validation;
using Channels.Domain.Imports;
using Channels.Domain.Marketplaces;
using FluentValidation;
using SharedKernel;
using SharedKernel.Tenancy;

namespace Channels.Application.Imports.ListProductImportRuns;

/// <summary>
/// Tenant'ın belirtilen pazaryerindeki son ürün import run'larını listeler.
/// </summary>
/// <remarks>
/// <para><b>Amaç:</b> Pazaryerleri ekranındaki import geçmişi görünümünü besler.</para>
/// <para><b>Ana akış:</b> Sorgu doğrulanır → pazaryeri anahtarı çözümlenir → tenant'ın run'ları
/// yeniden eskiye listelenir → <see cref="ProductImportRunSummaryDto"/> listesi döndürülür.</para>
/// <para><b>API:</b> GET /marketplaces/{key}/imports endpoint'i üzerinden kullanılır.</para>
/// </remarks>
public sealed class ListProductImportRunsHandler(
    IValidator<ListProductImportRunsQuery> validator,
    IProductImportRunRepository importRuns,
    ITenantContext tenantContext) : IListProductImportRunsHandler
{
    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<ProductImportRunSummaryDto>>> ExecuteAsync(
        ListProductImportRunsQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(query, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<ProductImportRunSummaryDto>>(validationResult.Error);
        }

        var marketplaceResult = Marketplace.FromCode(query.MarketplaceCode);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<ProductImportRunSummaryDto>>(marketplaceResult.Error);
        }

        var runs = await importRuns.ListRecentAsync(
            tenantContext.TenantId,
            marketplaceResult.Value,
            query.Limit,
            cancellationToken);

        return Result.Success<IReadOnlyList<ProductImportRunSummaryDto>>(
            runs.Select(run => run.ToSummaryDto()).ToList());
    }
}
