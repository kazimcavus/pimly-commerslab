namespace Channels.Application.Publications.EnqueuePublication;

/// <summary>Bir pazaryerine ürün yayını (publish) job'ı kuyruğa alma komutu.</summary>
public sealed record EnqueuePublicationCommand(string MarketplaceCode);
