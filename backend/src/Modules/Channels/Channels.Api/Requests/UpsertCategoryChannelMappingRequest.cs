namespace Channels.Api.Requests;

/// <summary>Catalog kategorisi ile harici kategori eşlemesi upsert isteği.</summary>
public sealed record UpsertCategoryChannelMappingRequest(string ExternalId);
