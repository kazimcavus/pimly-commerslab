namespace SharedKernel;

/// <summary>
/// Alan içinde gerçekleşen ve UTC zaman damgası taşıyan temel alan olayı.
/// </summary>
public abstract record DomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
