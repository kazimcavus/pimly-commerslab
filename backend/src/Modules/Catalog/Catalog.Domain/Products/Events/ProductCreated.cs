using SharedKernel;

namespace Catalog.Domain.Products.Events;

/// <summary>Yeni bir ürün oluşturulduğunda yayımlanan alan olayı.</summary>

/// <example>ProductId ve ModelCode "GOMlek-001" ile yayımlanır.</example>

public sealed record ProductCreated(Guid ProductId, string ModelCode) : DomainEvent;
