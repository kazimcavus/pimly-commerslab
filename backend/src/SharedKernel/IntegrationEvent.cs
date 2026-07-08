namespace SharedKernel;

/// <summary>
/// Modül sınırlarını aşan alan olayı. Aggregate tarafından diğer alan olayları gibi yayımlanır,
/// ancak kalıcılık sırasında outbox'a yazılıp diğer bounded context'lere iletilmesi hedeflenir.
/// </summary>
/// <remarks>
/// Domain olaylarının üst kümesidir; böylece mevcut <see cref="Entity{TId}.RaiseDomainEvent"/>
/// mekanizmasıyla toplanır. Outbox interceptor'ı yalnızca bu tipteki olayları seçip serialize eder.
/// </remarks>
public abstract record IntegrationEvent : DomainEvent;
