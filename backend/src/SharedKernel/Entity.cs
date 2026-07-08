namespace SharedKernel;

/// <summary>
/// Benzersiz kimlik ve alan olayları içeren temel varlık sınıfı.
/// </summary>
public abstract class Entity<TId> : IHasDomainEvents
    where TId : notnull
{
    private readonly List<DomainEvent> _domainEvents = [];

    public TId Id { get; protected set; } = default!;

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected Entity()
    {
    }

    protected Entity(TId id)
    {
        Id = id;
    }

    protected void RaiseDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (EqualityComparer<TId>.Default.Equals(Id, default!) ||
            EqualityComparer<TId>.Default.Equals(other.Id, default!))
        {
            return false;
        }

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();
}
