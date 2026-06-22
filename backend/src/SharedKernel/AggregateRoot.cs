namespace SharedKernel;

/// <summary>
/// Tutarlılık sınırını temsil eden ve alan olayları yöneten kök varlık sınıfı.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    protected AggregateRoot()
    {
    }

    protected AggregateRoot(TId id)
        : base(id)
    {
    }
}
