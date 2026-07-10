using SharedKernel;

namespace Channels.Domain.Publications;

/// <summary>
/// Tenant'ın satılabilir kalemlerini bir pazaryerine gönderme (yayın) job kaydı. ProductImportRun'ın
/// outbound aynasıdır: worker kuyruğu bu tablo üzerinden FOR UPDATE SKIP LOCKED ile beslenir. Kalemin
/// pazaryerindeki fiyatı Pricing'de kararlaştırılır (ChannelPrice); bu run onu yayına taşır.
/// </summary>
public sealed class ProductPublicationRun : AggregateRoot<Guid>
{
    /// <summary>Bir run'da saklanan en fazla hata kaydı sayısı.</summary>
    public const int MaxErrors = 500;

    private readonly List<ProductPublicationError> _errors = [];

    private ProductPublicationRun()
    {
        Marketplace = null!;
    }

    private ProductPublicationRun(Guid id, Guid tenantId, Marketplace marketplace, DateTimeOffset createdAt)
        : base(id)
    {
        TenantId = tenantId;
        Marketplace = marketplace;
        Status = PublicationStatus.Pending;
        CreatedAt = createdAt;
    }

    /// <summary>Gets yayını başlatan tenant kimliği.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets yayın yapılan pazaryeri.</summary>
    public Marketplace Marketplace { get; private set; }

    /// <summary>Gets job durumu.</summary>
    public PublicationStatus Status { get; private set; }

    /// <summary>Gets job oluşturulma zamanı.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Gets job başlama zamanı.</summary>
    public DateTimeOffset? StartedAt { get; private set; }

    /// <summary>Gets job tamamlanma zamanı.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Gets yayınlanmaya aday toplam kalem sayısı.</summary>
    public int? TotalItems { get; private set; }

    /// <summary>Gets işlenen kalem sayısı.</summary>
    public int ProcessedItems { get; private set; }

    /// <summary>Gets başarıyla yayımlanan kalem sayısı.</summary>
    public int PublishedItems { get; private set; }

    /// <summary>Gets hata alan kalem sayısı.</summary>
    public int FailedItems { get; private set; }

    /// <summary>Gets run düzeyindeki hata mesajı; yalnızca failed durumunda.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Gets kalem bazlı hata kayıtları (en fazla <see cref="MaxErrors"/>).</summary>
    public IReadOnlyCollection<ProductPublicationError> Errors => _errors.AsReadOnly();

    /// <summary>Yeni pending yayın job oluşturur.</summary>
    public static Result<ProductPublicationRun> Create(Guid tenantId, Marketplace marketplace, DateTimeOffset createdAt)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure<ProductPublicationRun>(Error.Validation("Tenant id is required."));
        }

        if (marketplace is null)
        {
            return Result.Failure<ProductPublicationRun>(Error.Validation("Marketplace is required."));
        }

        return Result.Success(new ProductPublicationRun(Guid.NewGuid(), tenantId, marketplace, createdAt));
    }

    /// <summary>Job'ı running durumuna alır.</summary>
    public Result MarkRunning(DateTimeOffset startedAt)
    {
        if (Status != PublicationStatus.Pending)
        {
            return Result.Failure(Error.Conflict("Only pending publication runs can be started."));
        }

        Status = PublicationStatus.Running;
        StartedAt = startedAt;
        return Result.Success();
    }

    /// <summary>İlerleme sayaçlarını günceller.</summary>
    public void UpdateProgress(int processedItems, int publishedItems, int failedItems, int? totalItems)
    {
        ProcessedItems = processedItems;
        PublishedItems = publishedItems;
        FailedItems = failedItems;
        TotalItems = totalItems;
    }

    /// <summary>
    /// Kalem bazlı hata kaydı ekler. Sınır aşıldıysa eklemez ve false döner;
    /// sayaçlar yine de <see cref="UpdateProgress"/> ile güncellenmelidir.
    /// </summary>
    public bool AddError(Guid productItemId, string message)
    {
        if (_errors.Count >= MaxErrors)
        {
            return false;
        }

        var normalizedMessage = string.IsNullOrWhiteSpace(message) ? "Publication failed." : message.Trim();
        if (normalizedMessage.Length > ProductPublicationError.MessageMaxLength)
        {
            normalizedMessage = normalizedMessage[..ProductPublicationError.MessageMaxLength];
        }

        _errors.Add(new ProductPublicationError(Guid.NewGuid(), productItemId, normalizedMessage));
        return true;
    }

    /// <summary>Job'ı tamamlar; hata alan kalem varsa completed_with_errors durumuna geçer.</summary>
    public Result MarkCompleted(DateTimeOffset completedAt)
    {
        if (Status != PublicationStatus.Running)
        {
            return Result.Failure(Error.Conflict("Only running publication runs can be completed."));
        }

        Status = FailedItems > 0 ? PublicationStatus.CompletedWithErrors : PublicationStatus.Completed;
        CompletedAt = completedAt;
        ErrorMessage = null;
        return Result.Success();
    }

    /// <summary>Job'ı altyapı hatası ile sonlandırır.</summary>
    public Result MarkFailed(DateTimeOffset completedAt, string errorMessage)
    {
        if (Status != PublicationStatus.Running)
        {
            return Result.Failure(Error.Conflict("Only running publication runs can be marked as failed."));
        }

        Status = PublicationStatus.Failed;
        CompletedAt = completedAt;
        ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? "Publication failed." : errorMessage.Trim();
        return Result.Success();
    }

    /// <summary>Job aktif (pending veya running) mi kontrol eder.</summary>
    public bool IsActive() =>
        Status is PublicationStatus.Pending or PublicationStatus.Running;
}
