using Channels.Application.Connections;
using Channels.Domain.Marketplaces;
using SharedKernel;

namespace Channels.Application.Imports;

/// <summary>Pazaryerindeki satıcı ürünlerini sayfalı olarak çeken istemci.</summary>
public interface IMarketplaceProductsClient
{
    /// <summary>Satıcının ürünlerinden bir sayfa getirir.</summary>
    Task<Result<MarketplaceProductPage>> FetchProductsPageAsync(
        Marketplace marketplace,
        MarketplaceCredentials credentials,
        int page,
        int size,
        CancellationToken cancellationToken = default);
}

/// <summary>Pazaryerinden dönen ürün sayfası.</summary>
public sealed record MarketplaceProductPage(
    long TotalElements,
    int TotalPages,
    int Page,
    int Size,
    IReadOnlyList<MarketplaceProductNode> Items);

/// <summary>Pazaryerindeki tek bir satılabilir ürün satırı (barkod düzeyi).</summary>
/// <remarks>Aynı ürünün varyantları <see cref="ProductMainId"/> ile gruplanır.</remarks>
public sealed record MarketplaceProductNode(
    string Barcode,
    string Title,
    string ProductMainId,
    string? Brand,
    string? StockCode,
    int Quantity,
    decimal ListPrice,
    decimal SalePrice,
    string? CurrencyType,
    string ExternalCategoryId,
    string? CategoryName,
    string? Description,
    bool Approved,
    IReadOnlyList<string> ImageUrls,
    IReadOnlyList<MarketplaceProductAttributeNode> Attributes);

/// <summary>Ürün satırındaki attribute değeri.</summary>
/// <remarks>
/// Varyant mı özellik mi ayrımı bu düğümde YOKTUR; kategori attribute cache'indeki
/// IsVariant/IsSlicer flag'leriyle <see cref="MarketplaceProductAttributeNode.ExternalAttributeId"/>
/// üzerinden join'lenerek belirlenir.
/// </remarks>
public sealed record MarketplaceProductAttributeNode(
    string ExternalAttributeId,
    string Name,
    string? ExternalValueId,
    string? Value,
    string? CustomValue);
