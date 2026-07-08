namespace Catalog.Infrastructure.Outbox;

/// <summary>
/// Kalıcılaştırılan integration olayı. Aggregate değişiklikleriyle aynı transaction içinde yazılır,
/// dispatcher worker tarafından okunup in-process handler'lara dağıtılır. Tenant genelinde
/// sorgulandığı için tenant query filter'ına dahil edilmez; <see cref="TenantId"/> açıkça taşınır.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>Gets olay kaydının benzersiz kimliği.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets olayı üreten tenant kimliği.</summary>
    public Guid TenantId { get; init; }

    /// <summary>Gets olayın tam CLR tip adı (dağıtımda handler eşlemesi için).</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>Gets olayın JSON gövdesi.</summary>
    public string Payload { get; init; } = string.Empty;

    /// <summary>Gets olayın gerçekleştiği UTC zaman.</summary>
    public DateTime OccurredOnUtc { get; init; }

    /// <summary>Gets or sets başarılı dağıtım zamanı; işlenmemişse null.</summary>
    public DateTime? ProcessedOnUtc { get; set; }

    /// <summary>Gets or sets dağıtım deneme sayısı.</summary>
    public int Attempts { get; set; }

    /// <summary>Gets or sets son dağıtım hatası; başarılıysa null.</summary>
    public string? Error { get; set; }
}
