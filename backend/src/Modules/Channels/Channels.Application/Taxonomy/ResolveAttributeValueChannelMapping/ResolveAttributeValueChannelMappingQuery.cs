namespace Channels.Application.Taxonomy.ResolveAttributeValueChannelMapping;

/// <summary>
/// Catalog attribute veya variant değeri için harici pazaryeri value kimliğini çözümlemek amacıyla
/// kullanılan dahili sorgu modeli.
/// </summary>
/// <param name="AttributeChannelMappingId">Değer eşlemesinin bağlı olduğu üst attribute kanal eşlemesi kimliği.</param>
/// <param name="CatalogValueId">Harici value id'si çözümlenecek Catalog değer kimliği.</param>
public sealed record ResolveAttributeValueChannelMappingQuery(
    Guid AttributeChannelMappingId,
    Guid CatalogValueId);
