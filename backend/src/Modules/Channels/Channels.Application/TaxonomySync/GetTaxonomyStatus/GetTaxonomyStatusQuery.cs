namespace Channels.Application.TaxonomySync.GetTaxonomyStatus;

/// <summary>
/// Belirli bir pazaryeri için taxonomy senkronizasyon özet durumunu sorgulayan istek modeli.
/// </summary>
/// <param name="MarketplaceCode">Durumu sorgulanacak pazaryerinin string anahtarı.</param>
public sealed record GetTaxonomyStatusQuery(string MarketplaceCode);
