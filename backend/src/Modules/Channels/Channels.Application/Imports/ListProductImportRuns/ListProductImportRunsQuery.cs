namespace Channels.Application.Imports.ListProductImportRuns;

/// <summary>Tenant'ın ürün import run'larını listeleme sorgusu.</summary>
public sealed record ListProductImportRunsQuery(string MarketplaceCode, int Limit = 20);
