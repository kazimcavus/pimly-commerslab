namespace Catalog.Domain.Products;

/// <summary>Varyant değeri anlık görüntüsü.</summary>

/// <example>"Renk" ekseni altında Name "Kırmızı".</example>

public sealed record VariantValue(Variant Variant, Guid Id, string Name, string? Code = null);
