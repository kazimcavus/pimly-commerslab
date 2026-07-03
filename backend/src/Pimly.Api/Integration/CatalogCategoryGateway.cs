using Catalog.Application.Categories.GetCategory;
using Channels.Application.Ports;

namespace Pimly.Api.Integration;

/// <summary>Catalog modülünden kategori okuma gateway implementasyonu.</summary>
internal sealed class CatalogCategoryGateway(IGetCategoryHandler getCategory) : ICatalogCategoryGateway
{
    public async Task<CatalogCategorySnapshot?> GetByIdAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var result = await getCategory.ExecuteAsync(new GetCategoryQuery(categoryId), cancellationToken);
        if (result.IsFailure)
        {
            return null;
        }

        var category = result.Value;
        return new CatalogCategorySnapshot(category.Id, category.Name, category.Code);
    }
}
