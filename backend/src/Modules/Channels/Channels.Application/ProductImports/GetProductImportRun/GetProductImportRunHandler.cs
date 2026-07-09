using Channels.Application.Contracts;
using Channels.Application.Validation;
using Channels.Domain.ProductImports;
using FluentValidation;
using SharedKernel;
using SharedKernel.Tenancy;

namespace Channels.Application.ProductImports.GetProductImportRun;

/// <summary>
/// Tenant'a ait tek bir ürün import run'ının ayrıntılı durumunu getirir.
/// </summary>
/// <remarks>
/// <para><b>Amaç:</b> Onboarding sihirbazının import ilerlemesini polling ile izlemesi için
/// sayaçları ve hata kayıtlarını döndürür.</para>
/// <para><b>Ön koşullar:</b> Geçerli pazaryeri anahtarı ve tenant'a ait mevcut bir run kimliği.</para>
/// <para><b>Ana akış:</b> Sorgu doğrulanır → pazaryeri çözümlenir → run getirilir → tenant ve
/// pazaryeri eşleşmesi doğrulanır → <see cref="ProductImportRunDto"/> döndürülür.</para>
/// <para><b>Hata durumları:</b> Doğrulama hatası, run bulunamadı veya başka tenant'a ait (NotFound).</para>
/// <para><b>API:</b> GET /marketplaces/{key}/imports/{runId} endpoint'i üzerinden kullanılır.</para>
/// </remarks>
public sealed class GetProductImportRunHandler(
    IValidator<GetProductImportRunQuery> validator,
    IProductImportRunRepository importRuns,
    ITenantContext tenantContext) : IGetProductImportRunHandler
{
    /// <inheritdoc/>
    public async Task<Result<ProductImportRunDto>> ExecuteAsync(
        GetProductImportRunQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(query, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<ProductImportRunDto>(validationResult.Error);
        }

        var marketplaceResult = Marketplace.FromCode(query.MarketplaceCode);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure<ProductImportRunDto>(marketplaceResult.Error);
        }

        var run = await importRuns.GetByIdAsync(query.RunId, cancellationToken);
        if (run is null
            || run.TenantId != tenantContext.TenantId
            || run.Marketplace != marketplaceResult.Value)
        {
            return Result.Failure<ProductImportRunDto>(Error.NotFound("Product import run not found."));
        }

        return Result.Success(run.ToDto());
    }
}
