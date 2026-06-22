namespace Catalog.Application.Attributes.UpdateAttributeValue;

/// <summary>Özellik değeri güncelleme isteğini temsil eder.</summary>
public sealed record UpdateAttributeValueCommand(Guid Id, string Name);
