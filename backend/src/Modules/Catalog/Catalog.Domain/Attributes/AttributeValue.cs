using SharedKernel;

namespace Catalog.Domain.Attributes;

/// <summary>
/// Bir özelliğe ait seçilebilir değeri temsil eden varlık.
/// </summary>
/// <example>
/// "Yaka Tipi" özelliği altında Name "V Yaka" olan bir değer.
/// </example>
public sealed class AttributeValue : Entity<Guid>
{
    private AttributeValue()
    {
    }

    internal AttributeValue(Guid id, string name)

        : base(id)
    {
        Name = name;
    }

    /// <summary>Gets değerin görünen adı.</summary>
    public string Name { get; private set; } = string.Empty;

    internal void Update(string name)
    {
        Name = name;
    }
}
