namespace Inventory.Application.Contracts;

/// <summary>Kalem stok seviyesi DTO'su.</summary>
public sealed record StockLevelDto(
    Guid ProductItemId,
    int Quantity,
    DateTimeOffset UpdatedAt);
