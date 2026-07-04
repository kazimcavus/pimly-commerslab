using Channels.Application.Contracts;
using Channels.Application.Validation;
using Channels.Domain;
using Channels.Domain.Connections;
using Channels.Domain.Marketplaces;
using Channels.Domain.ProductImports;
using FluentValidation;
using SharedKernel;
using SharedKernel.Tenancy;

namespace Channels.Application.ProductImports.EnqueueProductImport;

/// <summary>
/// Tenant'ın pazaryerindeki ürünlerini içe aktarmak için yeni bir import job'ı kuyruğa alır.
/// </summary>
/// <remarks>
/// <para><b>Amaç:</b> Onboarding "ürünlerimi çek" akışının giriş noktası; worker tarafından işlenecek
/// <see cref="ProductImportRun"/> kaydı oluşturur.</para>
/// <para><b>Ön koşullar:</b> Pazaryeri kayıtlı ve aktif; tenant'ın etkin bir bağlantısı (SellerId +
/// ApiKey + ApiSecret dolu) olmalı; tenant için aynı pazaryerinde aktif başka import olmamalı.</para>
/// <para><b>Ana akış:</b> Komut doğrulanır → pazaryeri çözümlenir → bağlantı doğrulanır → aktif run
/// kontrol edilir → yeni run oluşturulur → kaydedilir → <see cref="ProductImportRunDto"/> döndürülür.</para>
/// <para><b>Hata durumları:</b> Geçersiz/pasif pazaryeri (Validation), bağlantı eksik/pasif (NotFound /
/// Validation), eşzamanlı import (Conflict).</para>
/// <para><b>API:</b> POST /marketplaces/{key}/imports endpoint'i üzerinden kullanılır.</para>
/// </remarks>
public sealed class EnqueueProductImportHandler(
    IValidator<EnqueueProductImportCommand> validator,
    IMarketplaceConnectionRepository connections,
    IProductImportRunRepository importRuns,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IEnqueueProductImportHandler
{
    /// <inheritdoc/>
    public async Task<Result<ProductImportRunDto>> ExecuteAsync(
        EnqueueProductImportCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<ProductImportRunDto>(validationResult.Error);
        }

        var marketplaceResult = Marketplace.FromCode(command.MarketplaceCode);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure<ProductImportRunDto>(marketplaceResult.Error);
        }

        var marketplace = marketplaceResult.Value;

        var connection = await connections.GetByMarketplaceAsync(marketplace, cancellationToken);
        if (connection is null)
        {
            return Result.Failure<ProductImportRunDto>(
                Error.NotFound("Marketplace connection is required before importing products."));
        }

        if (!connection.IsEnabled)
        {
            return Result.Failure<ProductImportRunDto>(
                Error.Validation("Marketplace connection is disabled."));
        }

        if (string.IsNullOrWhiteSpace(connection.SellerId) || string.IsNullOrWhiteSpace(connection.ApiSecret))
        {
            return Result.Failure<ProductImportRunDto>(
                Error.Validation("Marketplace connection requires seller id and api secret for product import."));
        }

        var activeRun = await importRuns.GetActiveForTenantAsync(
            tenantContext.TenantId,
            marketplace,
            cancellationToken);

        if (activeRun is not null)
        {
            return Result.Failure<ProductImportRunDto>(
                Error.Conflict("A product import is already pending or running for this marketplace."));
        }

        var createResult = ProductImportRun.Create(
            tenantContext.TenantId,
            marketplace,
            timeProvider.GetUtcNow());

        if (createResult.IsFailure)
        {
            return Result.Failure<ProductImportRunDto>(createResult.Error);
        }

        await importRuns.AddAsync(createResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(createResult.Value.ToDto());
    }
}
