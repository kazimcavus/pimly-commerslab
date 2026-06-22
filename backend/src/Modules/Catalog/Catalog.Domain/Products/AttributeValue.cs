namespace Catalog.Domain.Products;

/// <summary>Özellik değeri anlık görüntüsü.</summary>

/// <example>"Malzeme" özelliği altında Name "Pamuk".</example>

public sealed record AttributeValue(Attribute Attribute, Guid Id, string Name);
