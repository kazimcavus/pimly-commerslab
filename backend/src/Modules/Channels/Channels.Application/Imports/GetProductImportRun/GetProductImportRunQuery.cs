namespace Channels.Application.Imports.GetProductImportRun;

/// <summary>Ürün import run ayrıntısı sorgusu.</summary>
public sealed record GetProductImportRunQuery(string MarketplaceCode, Guid RunId);
