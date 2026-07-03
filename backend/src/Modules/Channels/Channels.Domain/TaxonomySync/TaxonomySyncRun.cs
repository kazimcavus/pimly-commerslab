using Channels.Domain.Marketplaces;
using SharedKernel;

namespace Channels.Domain.TaxonomySync;

/// <summary>Pazaryeri kategori ağacı sync job kaydı.</summary>
public sealed class TaxonomySyncRun : AggregateRoot<Guid>
{
    private TaxonomySyncRun()
    {
        Marketplace = null!;
    }

    private TaxonomySyncRun(Guid id, Marketplace marketplace, DateTimeOffset createdAt)
        : base(id)
    {
        Marketplace = marketplace;
        Status = TaxonomySyncStatus.Pending;
        CreatedAt = createdAt;
    }

    /// <summary>Gets sync edilen pazaryeri anahtarı.</summary>
    public Marketplace Marketplace { get; private set; }

    /// <summary>Gets job durumu.</summary>
    public TaxonomySyncStatus Status { get; private set; }

    /// <summary>Gets job oluşturulma zamanı.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Gets job başlama zamanı.</summary>
    public DateTimeOffset? StartedAt { get; private set; }

    /// <summary>Gets job tamamlanma zamanı.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Gets işlenen kategori sayısı.</summary>
    public int ProcessedCount { get; private set; }

    /// <summary>Gets tahmini toplam kategori sayısı.</summary>
    public int? TotalEstimate { get; private set; }

    /// <summary>Gets hata mesajı; yalnızca failed durumunda.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Yeni pending sync job oluşturur.</summary>
    public static Result<TaxonomySyncRun> Create(Marketplace marketplace, DateTimeOffset createdAt) =>
        Result.Success(new TaxonomySyncRun(Guid.NewGuid(), marketplace, createdAt));

    /// <summary>Job'ı running durumuna alır.</summary>
    public Result MarkRunning(DateTimeOffset startedAt)
    {
        if (Status != TaxonomySyncStatus.Pending)
        {
            return Result.Failure(Error.Conflict("Only pending taxonomy sync runs can be started."));
        }

        Status = TaxonomySyncStatus.Running;
        StartedAt = startedAt;
        return Result.Success();
    }

    /// <summary>İlerleme sayaçlarını günceller.</summary>
    public void UpdateProgress(int processedCount, int? totalEstimate)
    {
        ProcessedCount = processedCount;
        TotalEstimate = totalEstimate;
    }

    /// <summary>Job'ı başarıyla tamamlar.</summary>
    public Result MarkCompleted(DateTimeOffset completedAt, int processedCount)
    {
        if (Status != TaxonomySyncStatus.Running)
        {
            return Result.Failure(Error.Conflict("Only running taxonomy sync runs can be completed."));
        }

        Status = TaxonomySyncStatus.Completed;
        CompletedAt = completedAt;
        ProcessedCount = processedCount;
        TotalEstimate = processedCount;
        ErrorMessage = null;
        return Result.Success();
    }

    /// <summary>Job'ı hata ile sonlandırır.</summary>
    public Result MarkFailed(DateTimeOffset completedAt, string errorMessage)
    {
        if (Status != TaxonomySyncStatus.Running)
        {
            return Result.Failure(Error.Conflict("Only running taxonomy sync runs can be marked as failed."));
        }

        Status = TaxonomySyncStatus.Failed;
        CompletedAt = completedAt;
        ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? "Taxonomy sync failed." : errorMessage.Trim();
        return Result.Success();
    }

    /// <summary>Job aktif (pending veya running) mi kontrol eder.</summary>
    public bool IsActive() =>
        Status is TaxonomySyncStatus.Pending or TaxonomySyncStatus.Running;
}
