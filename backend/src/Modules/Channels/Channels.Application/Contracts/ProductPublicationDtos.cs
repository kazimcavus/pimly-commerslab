namespace Channels.Application.Contracts;

/// <summary>Ürün yayın job özet DTO'su; liste görünümleri için sayaçları taşır.</summary>
public sealed record ProductPublicationRunSummaryDto(
    Guid Id,
    string MarketplaceCode,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    int? TotalItems,
    int ProcessedItems,
    int PublishedItems,
    int FailedItems);

/// <summary>Ürün yayın job ayrıntı DTO'su; hata kayıtlarını gömülü taşır.</summary>
public sealed record ProductPublicationRunDto(
    Guid Id,
    string MarketplaceCode,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    int? TotalItems,
    int ProcessedItems,
    int PublishedItems,
    int FailedItems,
    string? ErrorMessage,
    IReadOnlyList<ProductPublicationErrorDto> Errors);

/// <summary>Ürün yayın hata kaydı DTO'su.</summary>
public sealed record ProductPublicationErrorDto(
    Guid ProductItemId,
    string Message);
