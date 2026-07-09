using Channels.Application.Contracts;
using Channels.Application.Validation;
using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.ExternalCatalog;
using Channels.Domain.TaxonomySync;
using FluentValidation;
using SharedKernel;

namespace Channels.Application.TaxonomySync.GetTaxonomySyncRun;

/// <summary>
/// Belirli bir taxonomy senkronizasyon çalıştırmasının ayrıntılı durumunu getirir.
/// </summary>
/// <remarks>
/// <para><b>Amaç:</b> Tek bir sync run'ın ilerleme, durum ve zaman damgası bilgilerini izlemek için
/// kullanılır; manuel sync tetiklendikten sonra polling senaryolarında yararlanılır.</para>
/// <para><b>Ön koşullar:</b> Geçerli pazaryeri anahtarı ve mevcut bir <see cref="TaxonomySyncRun"/> kimliği.</para>
/// <para><b>Ana akış:</b> Sorgu doğrulanır → pazaryeri çözümlenir → run kimliği ile kayıt getirilir →
/// pazaryeri eşleşmesi kontrol edilir → <see cref="TaxonomySyncRunDto"/> döndürülür.</para>
/// <para><b>Hata durumları:</b> Doğrulama hatası, geçersiz pazaryeri, run bulunamadı veya pazaryeri
/// uyuşmazlığı (NotFound).</para>
/// <para><b>API:</b> Herkese açık HTTP API endpoint'i üzerinden kullanılır.</para>
/// </remarks>
public sealed class GetTaxonomySyncRunHandler(
    IValidator<GetTaxonomySyncRunQuery> validator,
    ITaxonomySyncRunRepository syncRuns) : IGetTaxonomySyncRunHandler
{
    /// <inheritdoc/>
    public async Task<Result<TaxonomySyncRunDto>> ExecuteAsync(
        GetTaxonomySyncRunQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(query, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<TaxonomySyncRunDto>(validationResult.Error);
        }

        var marketplaceResult = Marketplace.FromCode(query.MarketplaceCode);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure<TaxonomySyncRunDto>(marketplaceResult.Error);
        }

        var marketplace = marketplaceResult.Value;

        var syncRun = await syncRuns.GetByIdAsync(query.SyncRunId, cancellationToken);
        if (syncRun is null || syncRun.Marketplace != marketplace)
        {
            return Result.Failure<TaxonomySyncRunDto>(Error.NotFound("Taxonomy sync run not found."));
        }

        return Result.Success(syncRun.ToDto(marketplace));
    }
}
