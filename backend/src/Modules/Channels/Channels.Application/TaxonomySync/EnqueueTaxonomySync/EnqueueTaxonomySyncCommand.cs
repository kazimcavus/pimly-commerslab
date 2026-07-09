using SharedKernel;

namespace Channels.Application.TaxonomySync.EnqueueTaxonomySync;

/// <summary>
/// Belirli bir pazaryeri için taxonomy senkronizasyon job'ının kuyruğa alınmasını talep eden komut.
/// </summary>
/// <param name="Marketplace">Senkronize edilecek pazaryerinin benzersiz anahtarı.</param>
public sealed record EnqueueTaxonomySyncCommand(Marketplace Marketplace);
