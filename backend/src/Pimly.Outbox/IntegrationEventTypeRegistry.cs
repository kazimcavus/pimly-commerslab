using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Pimly.Outbox;

/// <summary>
/// Outbox'ta saklanan tam tip adını CLR tipine çözer. Modülün domain assembly'sinden taranan
/// integration olay tipleriyle doldurulur; sürümden bağımsızdır (AssemblyQualifiedName yerine FullName).
/// </summary>
/// <typeparam name="TDbContext">Registry'nin ait olduğu modül DbContext'i.</typeparam>
/// <remarks>
/// Context başına ayrı tutulur: bir modülün processor'ı yalnızca kendi yazdığı olayları çözer,
/// böylece modüller arası tip sızıntısı olmaz.
/// </remarks>
public sealed class IntegrationEventTypeRegistry<TDbContext>
    where TDbContext : DbContext, IOutboxDbContext
{
    private readonly IReadOnlyDictionary<string, Type> _typesByName;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationEventTypeRegistry{TDbContext}"/> class
    /// verilen integration olay tipleriyle.
    /// </summary>
    /// <param name="eventTypes">Kayda alınacak integration olay tipleri.</param>
    public IntegrationEventTypeRegistry(IEnumerable<Type> eventTypes)
    {
        _typesByName = eventTypes.ToDictionary(type => type.FullName!, type => type);
    }

    /// <summary>Verilen assembly'lerdeki somut integration olay tiplerini tarayarak registry kurar.</summary>
    /// <param name="assemblies">Taranacak assembly'ler (tipik olarak modülün domain assembly'si).</param>
    /// <returns>Taranan tiplerle dolu registry.</returns>
    public static IntegrationEventTypeRegistry<TDbContext> FromAssemblies(
        params System.Reflection.Assembly[] assemblies)
    {
        var eventTypes = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsAbstract: false } && typeof(IntegrationEvent).IsAssignableFrom(type));

        return new IntegrationEventTypeRegistry<TDbContext>(eventTypes);
    }

    /// <summary>Tam tip adını CLR tipine çözer; bilinmiyorsa null.</summary>
    /// <param name="typeName">Çözümlenecek tam tip adı.</param>
    /// <returns>Bulunan CLR tipi veya null.</returns>
    public Type? Resolve(string typeName) =>
        _typesByName.TryGetValue(typeName, out var type) ? type : null;
}
