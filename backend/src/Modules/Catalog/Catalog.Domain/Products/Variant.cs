using Catalog.Domain.Variants;

namespace Catalog.Domain.Products;

/// <summary>Ürün oluşturulurken katalog varyant türünden alınan eksen tanım anlık görüntüsü.</summary>

/// <example>Name "Renk", SelectionStyle Color, Slicer true.</example>

public sealed record Variant(

    Guid Id,

    string Name,

    SelectionStyle SelectionStyle,

    bool Slicer = false);
