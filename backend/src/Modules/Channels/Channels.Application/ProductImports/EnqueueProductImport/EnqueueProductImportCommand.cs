namespace Channels.Application.ProductImports.EnqueueProductImport;

/// <summary>Pazaryerinden ürün import job'ı kuyruğa alma komutu.</summary>
public sealed record EnqueueProductImportCommand(string MarketplaceCode);
