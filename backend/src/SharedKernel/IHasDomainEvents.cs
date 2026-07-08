namespace SharedKernel;

/// <summary>
/// Alan olayı taşıyan varlıklar için generic olmayan erişim. Kalıcılık katmanının,
/// aggregate tipini bilmeden izlenen varlıklardan olayları toplamasını sağlar.
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>Gets henüz temizlenmemiş alan olayları.</summary>
    IReadOnlyCollection<DomainEvent> DomainEvents { get; }

    /// <summary>Toplanan alan olaylarını temizler.</summary>
    void ClearDomainEvents();
}
