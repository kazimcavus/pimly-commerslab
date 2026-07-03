namespace Channels.Application.TaxonomySync.GetTaxonomySyncRun;

/// <summary>
/// Belirli bir taxonomy senkronizasyon çalıştırmasının durumunu sorgulayan istek modeli.
/// </summary>
/// <param name="MarketplaceCode">Sync run'ın ait olduğu pazaryerinin string anahtarı.</param>
/// <param name="SyncRunId">Sorgulanacak taxonomy sync run'ının benzersiz kimliği.</param>
public sealed record GetTaxonomySyncRunQuery(string MarketplaceCode, Guid SyncRunId);
