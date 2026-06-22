namespace Catalog.Application.Variants.UpdateVariantType;

/// <summary>Mevcut bir varyant türünü güncelleme isteğini temsil eder.</summary>
public sealed record UpdateVariantTypeCommand(Guid Id, string Name, string SelectionStyle, int SortOrder, bool Slicer);
