namespace Channels.Api.Requests;

/// <summary>Catalog attribute/variant ile harici attribute alan eşlemesi upsert isteği.</summary>
public sealed record UpsertAttributeChannelMappingRequest(
    string SourceType,
    Guid CatalogSourceId,
    string ExternalAttributeId);

/// <summary>Değer eşlemesi upsert girdisi.</summary>
public sealed record UpsertAttributeValueChannelMappingEntry(
    Guid CatalogValueId,
    string ExternalValueId);

/// <summary>Attribute/variant değer eşlemeleri toplu upsert isteği.</summary>
public sealed record UpsertAttributeValueChannelMappingsRequest(
    IReadOnlyList<UpsertAttributeValueChannelMappingEntry> Values);
