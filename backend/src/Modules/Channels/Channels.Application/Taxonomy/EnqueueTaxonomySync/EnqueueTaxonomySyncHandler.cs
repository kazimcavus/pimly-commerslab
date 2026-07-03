using Channels.Application.Contracts;
using Channels.Domain;
using Channels.Domain.Marketplaces;
using Channels.Domain.Taxonomy;
using SharedKernel;

namespace Channels.Application.Taxonomy.EnqueueTaxonomySync;

/// <summary>
/// Belirtilen pazaryeri için yeni bir taxonomy senkronizasyon çalıştırmasını kuyruğa alır.
/// </summary>
/// <remarks>
/// <para><b>Amaç:</b> Manuel veya zamanlanmış tetikleyiciler aracılığıyla pazaryeri kategori ağacının
/// arka planda indirilmesi için <see cref="TaxonomySyncRun"/> kaydı oluşturur.</para>
/// <para><b>Ön koşullar:</b> Pazaryeri kayıtlı ve aktif olmalıdır; aynı pazaryeri için bekleyen veya
/// çalışan başka bir sync olmamalıdır.</para>
/// <para><b>Ana akış:</b> Pazaryeri doğrulanır → aktif sync kontrol edilir → yeni run oluşturulur →
/// depoya eklenir ve kaydedilir → <see cref="TaxonomySyncRunDto"/> döndürülür.</para>
/// <para><b>Hata durumları:</b> Geçersiz/pasif pazaryeri (Validation), eşzamanlı sync (Conflict),
/// domain oluşturma hataları.</para>
/// <para><b>API:</b> Herkese açık HTTP API üzerinden manuel sync tetikleme için kullanılır;
/// <see cref="RunScheduledTaxonomySync.RunScheduledTaxonomySyncHandler"/> tarafından da dahili olarak çağrılır.</para>
/// </remarks>
public sealed class EnqueueTaxonomySyncHandler(
    ITaxonomySyncRunRepository syncRuns,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IEnqueueTaxonomySyncHandler
{
    /// <inheritdoc/>
    public async Task<Result<TaxonomySyncRunDto>> ExecuteAsync(
        EnqueueTaxonomySyncCommand command,
        CancellationToken cancellationToken = default)
    {
        var marketplaceResult = MarketplaceRegistry.GetByKey(command.MarketplaceKey);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure<TaxonomySyncRunDto>(marketplaceResult.Error);
        }

        var marketplace = marketplaceResult.Value;
        if (!marketplace.IsActive)
        {
            return Result.Failure<TaxonomySyncRunDto>(Error.Validation("Marketplace is not active."));
        }

        var activeSync = await syncRuns.GetActiveForMarketplaceAsync(marketplace.Key, cancellationToken);
        if (activeSync is not null)
        {
            return Result.Failure<TaxonomySyncRunDto>(
                Error.Conflict("A taxonomy sync is already pending or running for this marketplace."));
        }

        var createResult = TaxonomySyncRun.Create(marketplace.Key, timeProvider.GetUtcNow());
        if (createResult.IsFailure)
        {
            return Result.Failure<TaxonomySyncRunDto>(createResult.Error);
        }

        await syncRuns.AddAsync(createResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(createResult.Value.ToDto(marketplace.Key));
    }
}
