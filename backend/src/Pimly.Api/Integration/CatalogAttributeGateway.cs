using Catalog.Application.Attributes.GetAttribute;
using Catalog.Application.Categories.ListCategoryAttributes;
using Catalog.Domain;
using Channels.Application.Ports;

namespace Pimly.Api.Integration;

/// <summary>Catalog modülünden attribute okuma gateway implementasyonu.</summary>
internal sealed class CatalogAttributeGateway(
    IGetAttributeHandler getAttribute,
    IListCategoryAttributesHandler listCategoryAttributes,
    IAttributeRepository attributes) : ICatalogAttributeGateway
{
    public async Task<CatalogAttributeSnapshot?> GetByIdAsync(
        Guid attributeId,
        CancellationToken cancellationToken = default)
    {
        var result = await getAttribute.ExecuteAsync(new GetAttributeQuery(attributeId), cancellationToken);
        if (result.IsFailure)
        {
            return null;
        }

        var attribute = result.Value;
        return new CatalogAttributeSnapshot(attribute.Id, attribute.Key, attribute.Name);
    }

    public async Task<bool> AttributeBelongsToCategoryAsync(
        Guid categoryId,
        Guid attributeId,
        CancellationToken cancellationToken = default)
    {
        var result = await listCategoryAttributes.ExecuteAsync(
            new ListCategoryAttributesQuery(categoryId, 0, 0),
            cancellationToken);

        if (result.IsFailure)
        {
            return false;
        }

        return result.Value.Items.Any(item => item.AttributeId == attributeId);
    }

    public async Task<CatalogAttributeValueSnapshot?> GetValueByIdAsync(
        Guid attributeId,
        Guid valueId,
        CancellationToken cancellationToken = default)
    {
        var attribute = await attributes.GetByIdAsync(attributeId, cancellationToken);
        if (attribute is null)
        {
            return null;
        }

        var value = attribute.Values.FirstOrDefault(entry => entry.Id == valueId);
        if (value is null)
        {
            return null;
        }

        return new CatalogAttributeValueSnapshot(value.Id, attributeId, value.Name);
    }
}
