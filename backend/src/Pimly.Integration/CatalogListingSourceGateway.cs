using Catalog.Domain;
using Catalog.Domain.Products;
using Channels.Application.Listings.ContentSync;

namespace Pimly.Integration;

/// <summary>
/// Channels'ın Catalog'dan pazaryerine gidecek ürün içeriğini okuduğu ACL gateway implementasyonu.
/// Catalog aggregate'lerini modül-bağımsız anlık görüntülere çevirir.
/// </summary>
public sealed class CatalogListingSourceGateway(
    IProductRepository products,
    IBrandRepository brands) : ICatalogListingSourceGateway
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<CatalogListingSource>> GetAsync(
        IReadOnlyCollection<Guid> productItemIds,
        CancellationToken cancellationToken = default)
    {
        if (productItemIds.Count == 0)
        {
            return [];
        }

        var wanted = productItemIds.ToHashSet();
        var loaded = await products.ListByItemIdsAsync(productItemIds, cancellationToken);

        // Marka adları ürün başına tekrar ettiği için tek turda önbelleklenir.
        var brandNames = new Dictionary<Guid, (string Name, string? Code)>();
        foreach (var brandId in loaded.Where(p => p.BrandId.HasValue).Select(p => p.BrandId!.Value).Distinct())
        {
            var brand = await brands.GetByIdAsync(brandId, cancellationToken);
            if (brand is not null)
            {
                brandNames[brandId] = (brand.Name, brand.Code);
            }
        }

        var sources = new List<CatalogListingSource>();

        foreach (var product in loaded)
        {
            var brand = product.BrandId.HasValue && brandNames.TryGetValue(product.BrandId.Value, out var found)
                ? found
                : default;

            var imageUrls = product.Images
                .OrderByDescending(image => image.IsPrimary)
                .ThenBy(image => image.SortOrder)
                .Select(image => image.Url)
                .ToList();

            foreach (var item in product.Items.Where(item => wanted.Contains(item.Id)))
            {
                sources.Add(new CatalogListingSource(
                    item.Id,
                    product.Id,
                    product.CategoryId,
                    product.Name,
                    product.Description,
                    brand.Name,
                    brand.Code,
                    product.ModelCode.Value,
                    item.Barcode,
                    item.Sku,
                    BuildSelections(product, item),
                    imageUrls));
            }
        }

        return sources;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Guid>> ListItemIdsByCategoriesAsync(
        IReadOnlyCollection<Guid> categoryIds,
        CancellationToken cancellationToken = default) =>
        products.ListItemIdsByCategoriesAsync(categoryIds, cancellationToken);

    /// <summary>
    /// Ürün düzeyi özellikler ile kalem düzeyi özellik/varyant seçimlerini tek listede birleştirir.
    /// Kalem seçimi ürün seçimini ezer (aynı özellik iki düzeyde tanımlıysa kalemdeki geçerlidir).
    /// </summary>
    private static List<CatalogListingSelection> BuildSelections(Product product, ProductItem item)
    {
        var byKey = new Dictionary<(bool IsVariant, Guid SourceId), CatalogListingSelection>();

        foreach (var attributeValue in product.AttributeValues)
        {
            byKey[(false, attributeValue.Attribute.Id)] = new CatalogListingSelection(
                false,
                attributeValue.Attribute.Id,
                attributeValue.Id,
                attributeValue.Name);
        }

        foreach (var attributeValue in item.AttributeValues)
        {
            byKey[(false, attributeValue.Attribute.Id)] = new CatalogListingSelection(
                false,
                attributeValue.Attribute.Id,
                attributeValue.Id,
                attributeValue.Name);
        }

        foreach (var variantValue in item.VariantValues)
        {
            byKey[(true, variantValue.Variant.Id)] = new CatalogListingSelection(
                true,
                variantValue.Variant.Id,
                variantValue.Id,
                variantValue.Name);
        }

        return [.. byKey.Values];
    }
}
