using Channels.Domain.Marketplaces;

namespace Channels.Application.Taxonomy.EnqueueTaxonomySync;

/// <summary>
/// Belirli bir pazaryeri için taxonomy senkronizasyon job'ının kuyruğa alınmasını talep eden komut.
/// </summary>
/// <param name="MarketplaceKey">Senkronize edilecek pazaryerinin benzersiz anahtarı.</param>
public sealed record EnqueueTaxonomySyncCommand(MarketplaceKey MarketplaceKey);
