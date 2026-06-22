using SharedKernel;

namespace Catalog.Domain.Attributes.Events;

/// <summary>
/// Yeni bir ürün özelliği tanımı oluşturulduğunda yayımlanan alan olayı.
/// </summary>
public sealed record AttributeCreated(Guid AttributeId, string Key) : DomainEvent;
