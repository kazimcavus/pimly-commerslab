namespace Channels.Application.ProductImports.GetProductImportRun;

/// <summary>Ürün import run ayrıntısı sorgusu.</summary>
public sealed record GetProductImportRunQuery(string MarketplaceCode, Guid RunId);
