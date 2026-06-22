namespace Catalog.Domain.Products;

/// <summary>Özellik tanım anlık görüntüsü.</summary>

/// <example>Key "malzeme", Name "Malzeme".</example>

public sealed record Attribute(Guid Id, string Key, string Name);
