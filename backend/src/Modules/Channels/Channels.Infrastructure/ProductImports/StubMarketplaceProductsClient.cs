using Channels.Application.Connections;
using Channels.Application.ProductImports;
using Channels.Domain.Marketplaces;
using SharedKernel;

namespace Channels.Infrastructure.ProductImports;

/// <summary>
/// Geliştirme/test için deterministik sahte satıcı kataloğu. Stub taksonomi id'lerine bağlıdır:
/// "221" Gömlek (Beden + Renk varianter, Renk slicer, Kumaş özellik) ve "111" Akıllı Telefon.
/// Görsel listeleri bilinçli olarak boştur (testlerde ağ erişimi olmaması için).
/// </summary>
internal sealed class StubMarketplaceProductsClient : IMarketplaceProductsClient
{
    private static readonly IReadOnlyList<MarketplaceProductNode> Catalog = BuildCatalog();

    /// <inheritdoc/>
    public Task<Result<MarketplaceProductPage>> FetchProductsPageAsync(
        Marketplace marketplace,
        MarketplaceCredentials credentials,
        int page,
        int size,
        CancellationToken cancellationToken = default)
    {
        _ = marketplace;
        _ = credentials;
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedSize = Math.Max(1, size);
        var items = Catalog.Skip(page * normalizedSize).Take(normalizedSize).ToList();
        var totalPages = (int)Math.Ceiling(Catalog.Count / (double)normalizedSize);

        return Task.FromResult(Result.Success(new MarketplaceProductPage(
            Catalog.Count,
            totalPages,
            page,
            normalizedSize,
            items)));
    }

    private static List<MarketplaceProductNode> BuildCatalog()
    {
        var catalog = new List<MarketplaceProductNode>();

        // GOMLEK-001: Renk (slicer) x Beden kombinasyonları — 2 renk x 2 beden = 4 barkod.
        var gomlekCombos = new (string Barcode, string RenkValueId, string Renk, string BedenValueId, string Beden)[]
        {
            ("8680000000011", "val-mavi", "Mavi", "val-s", "S"),
            ("8680000000012", "val-mavi", "Mavi", "val-m", "M"),
            ("8680000000013", "val-beyaz-gomlek", "Beyaz", "val-s", "S"),
            ("8680000000014", "val-beyaz-gomlek", "Beyaz", "val-m", "M"),
        };

        foreach (var combo in gomlekCombos)
        {
            catalog.Add(new MarketplaceProductNode(
                combo.Barcode,
                "Klasik Yaka Gömlek",
                "GOMLEK-001",
                "Pimly",
                $"GOMLEK-001-{combo.Renk.ToUpperInvariant()}-{combo.Beden}",
                Quantity: 25,
                ListPrice: 599.90m,
                SalePrice: 449.90m,
                CurrencyType: "TRY",
                ExternalCategoryId: "221",
                CategoryName: "Gömlek",
                Description: "Pamuklu klasik yaka gömlek.",
                Approved: true,
                ImageUrls: [],
                Attributes:
                [
                    new MarketplaceProductAttributeNode("attr-renk-gomlek", "Renk", combo.RenkValueId, combo.Renk, null),
                    new MarketplaceProductAttributeNode("attr-beden", "Beden", combo.BedenValueId, combo.Beden, null),
                    new MarketplaceProductAttributeNode("attr-kumas", "Kumaş", "val-pamuk", "Pamuk", null),
                ]));
        }

        // GOMLEK-002: tek renk (Mavi) x 2 beden — slicer tek ürün üretir.
        var gomlek2Combos = new (string Barcode, string BedenValueId, string Beden)[]
        {
            ("8680000000021", "val-s", "S"),
            ("8680000000022", "val-l", "L"),
        };

        foreach (var combo in gomlek2Combos)
        {
            catalog.Add(new MarketplaceProductNode(
                combo.Barcode,
                "Oxford Gömlek",
                "GOMLEK-002",
                "Oxford",
                $"GOMLEK-002-MAVI-{combo.Beden}",
                Quantity: 10,
                ListPrice: 499.90m,
                SalePrice: 499.90m,
                CurrencyType: "TRY",
                ExternalCategoryId: "221",
                CategoryName: "Gömlek",
                Description: "Oxford dokuma gömlek.",
                Approved: true,
                ImageUrls: [],
                Attributes:
                [
                    new MarketplaceProductAttributeNode("attr-renk-gomlek", "Renk", "val-mavi", "Mavi", null),
                    new MarketplaceProductAttributeNode("attr-beden", "Beden", combo.BedenValueId, combo.Beden, null),
                    new MarketplaceProductAttributeNode("attr-kumas", "Kumaş", "val-polyester", "Polyester", null),
                ]));
        }

        // TELEFON-001: Renk varianter (slicer) + Hafıza/Marka özellikler — 2 barkod.
        var telefonCombos = new (string Barcode, string RenkValueId, string Renk)[]
        {
            ("8680000000031", "val-siyah", "Siyah"),
            ("8680000000032", "val-beyaz", "Beyaz"),
        };

        foreach (var combo in telefonCombos)
        {
            catalog.Add(new MarketplaceProductNode(
                combo.Barcode,
                "Pimly Akıllı Telefon 128 GB",
                "TELEFON-001",
                "Pimly",
                $"TELEFON-001-{combo.Renk.ToUpperInvariant()}",
                Quantity: 5,
                ListPrice: 15999.00m,
                SalePrice: 14999.00m,
                CurrencyType: "TRY",
                ExternalCategoryId: "111",
                CategoryName: "Akıllı Telefon",
                Description: "128 GB akıllı telefon.",
                Approved: true,
                ImageUrls: [],
                Attributes:
                [
                    new MarketplaceProductAttributeNode("attr-renk", "Renk", combo.RenkValueId, combo.Renk, null),
                    new MarketplaceProductAttributeNode("attr-hafiza", "Hafıza", "val-128", "128 GB", null),
                    new MarketplaceProductAttributeNode("attr-marka", "Marka", null, null, "Pimly Tech"),
                ]));
        }

        return catalog;
    }
}
