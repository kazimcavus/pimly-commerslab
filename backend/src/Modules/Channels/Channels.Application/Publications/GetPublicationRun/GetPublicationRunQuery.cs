namespace Channels.Application.Publications.GetPublicationRun;

/// <summary>Ürün yayın run ayrıntısı sorgusu.</summary>
public sealed record GetPublicationRunQuery(string MarketplaceCode, Guid RunId);
