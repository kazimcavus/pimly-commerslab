using SharedKernel;

namespace Catalog.Domain.Categories.Events;

/// <summary>
/// Yeni bir kategori oluşturulduğunda yayımlanan alan olayı.
/// </summary>
public sealed record CategoryCreated(Guid CategoryId, string Name) : DomainEvent;
