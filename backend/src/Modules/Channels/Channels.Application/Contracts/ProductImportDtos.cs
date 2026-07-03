namespace Channels.Application.Contracts;

/// <summary>Ürün import job özet DTO'su; liste görünümleri için sayaçları taşır.</summary>
public sealed record ProductImportRunSummaryDto(
    Guid Id,
    string MarketplaceCode,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    int? TotalProducts,
    int ProcessedProducts,
    int ImportedProducts,
    int SkippedProducts,
    int FailedProducts);

/// <summary>Ürün import job ayrıntı DTO'su; hata kayıtlarını gömülü taşır.</summary>
public sealed record ProductImportRunDto(
    Guid Id,
    string MarketplaceCode,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    int? TotalProducts,
    int ProcessedProducts,
    int ImportedProducts,
    int SkippedProducts,
    int FailedProducts,
    string? ErrorMessage,
    IReadOnlyList<ProductImportErrorDto> Errors);

/// <summary>Ürün import hata kaydı DTO'su.</summary>
public sealed record ProductImportErrorDto(
    string ProductMainId,
    string? Barcode,
    string Message);
