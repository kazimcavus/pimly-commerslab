namespace Catalog.Application.Contracts;

/// <summary>
/// Kalemin bir fiyat tanımındaki tutar DTO'su.
/// Tanım adı okuma kolaylığı için fiyat tanımından join edilir.
/// </summary>
public sealed record ItemPriceDto(
    Guid Id,
    Guid ProductItemId,
    Guid PriceDefinitionId,
    string DefinitionName,
    decimal Amount,
    string Currency,
    DateTimeOffset UpdatedAt);
