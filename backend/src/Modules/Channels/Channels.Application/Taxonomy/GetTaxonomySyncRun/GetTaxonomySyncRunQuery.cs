namespace Channels.Application.Taxonomy.GetTaxonomySyncRun;

/// <summary>
/// Belirli bir taxonomy senkronizasyon çalıştırmasının durumunu sorgulayan istek modeli.
/// </summary>
/// <param name="MarketplaceKey">Sync run'ın ait olduğu pazaryerinin string anahtarı.</param>
/// <param name="SyncRunId">Sorgulanacak taxonomy sync run'ının benzersiz kimliği.</param>
public sealed record GetTaxonomySyncRunQuery(string MarketplaceKey, Guid SyncRunId);
