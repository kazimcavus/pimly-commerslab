using Channels.Application.Contracts;
using Channels.Application.Validation;
using Channels.Domain.Marketplaces;
using Channels.Domain.Taxonomy;
using FluentValidation;
using SharedKernel;

namespace Channels.Application.Taxonomy.GetTaxonomyStatus;

/// <summary>
/// Belirtilen pazaryeri için taxonomy senkronizasyon özet durumunu (aktif sync, son tamamlanan run,
/// cache'lenmiş kategori sayısı) getirir.
/// </summary>
/// <remarks>
/// <para><b>Amaç:</b> Yönetim arayüzüne pazaryeri taxonomy sağlık durumunu sunar; sync devam ediyor mu,
/// son başarılı sync ne zaman, kaç kategori cache'lendi gibi bilgileri tek istekte toplar.</para>
/// <para><b>Ön koşullar:</b> Geçerli pazaryeri anahtarı; taxonomy cache dolu olmak zorunda değildir.</para>
/// <para><b>Ana akış:</b> Sorgu doğrulanır → pazaryeri çözümlenir → aktif sync ve son tamamlanan run
/// sorgulanır → cache'lenmiş kategori sayısı alınır → <see cref="TaxonomyStatusDto"/> oluşturulur.</para>
/// <para><b>Hata durumları:</b> Doğrulama hatası, geçersiz pazaryeri anahtarı, kayıtlı olmayan pazaryeri.</para>
/// <para><b>API:</b> Herkese açık HTTP API endpoint'i üzerinden kullanılır.</para>
/// </remarks>
public sealed class GetTaxonomyStatusHandler(
    IValidator<GetTaxonomyStatusQuery> validator,
    ITaxonomySyncRunRepository syncRuns,
    IExternalCategoryRepository externalCategories) : IGetTaxonomyStatusHandler
{
    /// <inheritdoc/>
    public async Task<Result<TaxonomyStatusDto>> ExecuteAsync(
        GetTaxonomyStatusQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(query, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<TaxonomyStatusDto>(validationResult.Error);
        }

        var keyResult = MarketplaceKey.Create(query.MarketplaceKey);
        if (keyResult.IsFailure)
        {
            return Result.Failure<TaxonomyStatusDto>(keyResult.Error);
        }

        var marketplaceResult = MarketplaceRegistry.GetByKey(keyResult.Value);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure<TaxonomyStatusDto>(marketplaceResult.Error);
        }

        var marketplace = marketplaceResult.Value;
        var activeSync = await syncRuns.GetActiveForMarketplaceAsync(marketplace.Key, cancellationToken);
        var lastCompleted = await syncRuns.GetLatestCompletedForMarketplaceAsync(marketplace.Key, cancellationToken);
        var cachedCount = await externalCategories.CountByMarketplaceAsync(marketplace.Key, cancellationToken);

        return Result.Success(new TaxonomyStatusDto(
            marketplace.Key.Value,
            activeSync is not null,
            activeSync?.Id,
            lastCompleted?.CompletedAt,
            cachedCount,
            lastCompleted?.ToDto(marketplace.Key)));
    }
}
