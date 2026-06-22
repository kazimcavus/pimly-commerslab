namespace Catalog.Application.Attributes.UpdateAttribute;

/// <summary>Mevcut bir özniteliği güncelleme isteğini temsil eder.</summary>
public sealed record UpdateAttributeCommand(Guid Id, string Name);
