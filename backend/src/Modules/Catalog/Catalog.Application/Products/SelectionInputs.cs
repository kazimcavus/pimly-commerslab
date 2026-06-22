namespace Catalog.Application.Products;

/// <summary>Ürün oluşturma/güncelleme isteklerinde özellik değeri girdisi.</summary>
public sealed record AttributeValueInput(Guid AttributeId, Guid AttributeValueId);

/// <summary>Ürün oluşturma isteklerinde eksen değeri girdisi.</summary>
public sealed record VariantValueInput(Guid VariantId, Guid VariantValueId);
