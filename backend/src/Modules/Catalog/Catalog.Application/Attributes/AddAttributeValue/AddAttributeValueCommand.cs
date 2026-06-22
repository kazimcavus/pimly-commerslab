namespace Catalog.Application.Attributes.AddAttributeValue;

/// <summary>Özelliğe yeni bir değer ekleme isteğini temsil eder.</summary>
public sealed record AddAttributeValueCommand(Guid AttributeId, string Name);
