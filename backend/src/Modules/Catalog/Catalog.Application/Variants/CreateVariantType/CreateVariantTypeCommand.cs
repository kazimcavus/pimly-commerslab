namespace Catalog.Application.Variants.CreateVariantType;

/// <summary>Yeni bir varyant türü oluşturma isteğini temsil eder.</summary>
public sealed record CreateVariantTypeCommand(string Name, string SelectionStyle, int SortOrder, bool Slicer = false);
