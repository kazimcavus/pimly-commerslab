using SharedKernel;

namespace Channels.Domain.ProductImports;

/// <summary>
/// Pazaryerinden ürün import job kaydı. TaxonomySyncRun deseninin tenant'lı karşılığıdır:
/// worker kuyruğu bu tablo üzerinden FOR UPDATE SKIP LOCKED ile beslenir.
/// </summary>
public sealed class ProductImportRun : AggregateRoot<Guid>
{
    /// <summary>Bir run'da saklanan en fazla hata kaydı sayısı.</summary>
    public const int MaxErrors = 500;

    private readonly List<ProductImportError> _errors = [];

    private ProductImportRun()
    {
        Marketplace = null!;
    }

    private ProductImportRun(Guid id, Guid tenantId, Marketplace marketplace, DateTimeOffset createdAt)
        : base(id)
    {
        TenantId = tenantId;
        Marketplace = marketplace;
        Status = ProductImportStatus.Pending;
        CreatedAt = createdAt;
    }

    /// <summary>Gets import'u başlatan tenant kimliği.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets import edilen pazaryeri.</summary>
    public Marketplace Marketplace { get; private set; }

    /// <summary>Gets job durumu.</summary>
    public ProductImportStatus Status { get; private set; }

    /// <summary>Gets job oluşturulma zamanı.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Gets job başlama zamanı.</summary>
    public DateTimeOffset? StartedAt { get; private set; }

    /// <summary>Gets job tamamlanma zamanı.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Gets pazaryerindeki toplam ürün grubu (productMainId) sayısı.</summary>
    public int? TotalProducts { get; private set; }

    /// <summary>Gets işlenen ürün grubu sayısı.</summary>
    public int ProcessedProducts { get; private set; }

    /// <summary>Gets başarıyla içe aktarılan ürün grubu sayısı.</summary>
    public int ImportedProducts { get; private set; }

    /// <summary>Gets zaten var olduğu için atlanan ürün grubu sayısı.</summary>
    public int SkippedProducts { get; private set; }

    /// <summary>Gets hata alan ürün grubu sayısı.</summary>
    public int FailedProducts { get; private set; }

    /// <summary>Gets run düzeyindeki hata mesajı; yalnızca failed durumunda.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Gets ürün bazlı hata kayıtları (en fazla <see cref="MaxErrors"/>).</summary>
    public IReadOnlyCollection<ProductImportError> Errors => _errors.AsReadOnly();

    /// <summary>Yeni pending import job oluşturur.</summary>
    public static Result<ProductImportRun> Create(Guid tenantId, Marketplace marketplace, DateTimeOffset createdAt)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure<ProductImportRun>(Error.Validation("Tenant id is required."));
        }

        return Result.Success(new ProductImportRun(Guid.NewGuid(), tenantId, marketplace, createdAt));
    }

    /// <summary>Job'ı running durumuna alır.</summary>
    public Result MarkRunning(DateTimeOffset startedAt)
    {
        if (Status != ProductImportStatus.Pending)
        {
            return Result.Failure(Error.Conflict("Only pending product import runs can be started."));
        }

        Status = ProductImportStatus.Running;
        StartedAt = startedAt;
        return Result.Success();
    }

    /// <summary>İlerleme sayaçlarını günceller.</summary>
    public void UpdateProgress(int processedProducts, int importedProducts, int skippedProducts, int failedProducts, int? totalProducts)
    {
        ProcessedProducts = processedProducts;
        ImportedProducts = importedProducts;
        SkippedProducts = skippedProducts;
        FailedProducts = failedProducts;
        TotalProducts = totalProducts;
    }

    /// <summary>
    /// Ürün bazlı hata kaydı ekler. Sınır aşıldıysa eklemez ve false döner;
    /// sayaçlar yine de <see cref="UpdateProgress"/> ile güncellenmelidir.
    /// </summary>
    public bool AddError(string productMainId, string? barcode, string message)
    {
        if (_errors.Count >= MaxErrors)
        {
            return false;
        }

        var normalizedMessage = string.IsNullOrWhiteSpace(message) ? "Import failed." : message.Trim();
        if (normalizedMessage.Length > ProductImportError.MessageMaxLength)
        {
            normalizedMessage = normalizedMessage[..ProductImportError.MessageMaxLength];
        }

        _errors.Add(new ProductImportError(
            Guid.NewGuid(),
            string.IsNullOrWhiteSpace(productMainId) ? "-" : productMainId.Trim(),
            string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim(),
            normalizedMessage));

        return true;
    }

    /// <summary>Job'ı tamamlar; hata alan grup varsa completed_with_errors durumuna geçer.</summary>
    public Result MarkCompleted(DateTimeOffset completedAt)
    {
        if (Status != ProductImportStatus.Running)
        {
            return Result.Failure(Error.Conflict("Only running product import runs can be completed."));
        }

        Status = FailedProducts > 0 ? ProductImportStatus.CompletedWithErrors : ProductImportStatus.Completed;
        CompletedAt = completedAt;
        ErrorMessage = null;
        return Result.Success();
    }

    /// <summary>Job'ı altyapı hatası ile sonlandırır.</summary>
    public Result MarkFailed(DateTimeOffset completedAt, string errorMessage)
    {
        if (Status != ProductImportStatus.Running)
        {
            return Result.Failure(Error.Conflict("Only running product import runs can be marked as failed."));
        }

        Status = ProductImportStatus.Failed;
        CompletedAt = completedAt;
        ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? "Product import failed." : errorMessage.Trim();
        return Result.Success();
    }

    /// <summary>Job aktif (pending veya running) mi kontrol eder.</summary>
    public bool IsActive() =>
        Status is ProductImportStatus.Pending or ProductImportStatus.Running;
}
