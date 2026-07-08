namespace Catalog.Infrastructure.Outbox;

/// <summary>
/// Outbox'ta saklanan tam tip adını CLR tipine çözer. Başlangıçta bilinen integration
/// olay tipleriyle doldurulur; sürümden bağımsız (AssemblyQualifiedName yerine FullName).
/// </summary>
public sealed class IntegrationEventTypeRegistry
{
    private readonly IReadOnlyDictionary<string, Type> _typesByName;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationEventTypeRegistry"/> class
    /// verilen integration olay tipleriyle.
    /// </summary>
    public IntegrationEventTypeRegistry(IEnumerable<Type> eventTypes)
    {
        _typesByName = eventTypes.ToDictionary(type => type.FullName!, type => type);
    }

    /// <summary>Tam tip adını CLR tipine çözer; bilinmiyorsa null.</summary>
    public Type? Resolve(string typeName) =>
        _typesByName.TryGetValue(typeName, out var type) ? type : null;
}
