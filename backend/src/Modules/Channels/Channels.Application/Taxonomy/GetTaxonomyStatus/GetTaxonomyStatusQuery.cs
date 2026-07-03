namespace Channels.Application.Taxonomy.GetTaxonomyStatus;

/// <summary>
/// Belirli bir pazaryeri için taxonomy senkronizasyon özet durumunu sorgulayan istek modeli.
/// </summary>
/// <param name="MarketplaceKey">Durumu sorgulanacak pazaryerinin string anahtarı.</param>
public sealed record GetTaxonomyStatusQuery(string MarketplaceKey);
