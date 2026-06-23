using SharedKernel;

namespace Catalog.Domain.Variants.Events;

/// <summary>
/// Yeni bir varyant türü oluşturulduğunda yayımlanan alan olayı.
/// </summary>
public sealed record VariantCreated(Guid VariantId, string Key) : DomainEvent;
