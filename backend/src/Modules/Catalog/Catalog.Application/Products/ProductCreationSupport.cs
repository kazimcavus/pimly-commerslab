using Catalog.Application.Products;
using Catalog.Domain;
using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using SharedKernel;
using ProductAttribute = Catalog.Domain.Products.Attribute;
using ProductAttributeValue = Catalog.Domain.Products.AttributeValue;

namespace Catalog.Application.Products;

/// <summary>Ürün oluşturma handler'ları için ortak yardımcılar.</summary>
internal static class ProductCreationSupport
{
    internal static async Task<Result<IReadOnlyList<ProductItemDraft>>> ResolveItemDraftsAsync(
        IVariantRepository variantTypes,
        IAttributeRepository attributes,
        IReadOnlyList<CreateProduct.CreateProductItemInput> items,
        CancellationToken cancellationToken)
    {
        var drafts = new List<ProductItemDraft>();
        foreach (var item in items)
        {
            var variantValuesResult = await ResolveVariantValuesAsync(
                variantTypes,
                item.VariantValueInputs,
                cancellationToken);

            if (variantValuesResult.IsFailure)
            {
                return Result.Failure<IReadOnlyList<ProductItemDraft>>(variantValuesResult.Error);
            }

            var attributeValuesResult = await ResolveAttributeValuesAsync(
                attributes,
                item.AttributeValueInputs,
                cancellationToken);

            if (attributeValuesResult.IsFailure)
            {
                return Result.Failure<IReadOnlyList<ProductItemDraft>>(attributeValuesResult.Error);
            }

            drafts.Add(new ProductItemDraft(
                item.Sku,
                item.Barcode,
                item.Gtin,
                item.Mpn,
                item.AxisValueEntryId,
                item.AxisValue,
                item.Price,
                item.CompareAtPrice,
                item.Stock,
                attributeValuesResult.Value,
                variantValuesResult.Value));
        }

        return Result.Success<IReadOnlyList<ProductItemDraft>>(drafts);
    }

    internal static async Task<Result<IReadOnlyList<Variant>>> ResolveVariantsAsync(
        IVariantRepository variantTypes,
        IReadOnlyList<Variant>? snapshots,
        CancellationToken cancellationToken)
    {
        if (snapshots is null || snapshots.Count == 0)
        {
            return Result.Success<IReadOnlyList<Variant>>([]);
        }

        var resolved = new List<Variant>();
        foreach (var snapshot in snapshots)
        {
            var variantType = await variantTypes.GetByIdAsync(snapshot.Id, cancellationToken);
            if (variantType is null)
            {
                return Result.Failure<IReadOnlyList<Variant>>(
                    Error.NotFound($"Variant type '{snapshot.Id}' not found."));
            }

            resolved.Add(new Variant(
                variantType.Id,
                variantType.Name,
                variantType.SelectionStyle,
                variantType.Slicer));
        }

        return Result.Success<IReadOnlyList<Variant>>(resolved);
    }

    internal static async Task<Result<IReadOnlyList<VariantValue>>> ResolveVariantValuesAsync(
        IVariantRepository variantTypes,
        IReadOnlyList<VariantValueInput>? inputs,
        CancellationToken cancellationToken)
    {
        if (inputs is null || inputs.Count == 0)
        {
            return Result.Success<IReadOnlyList<VariantValue>>([]);
        }

        var resolved = new List<VariantValue>();
        foreach (var input in inputs)
        {
            var variantType = await variantTypes.GetByIdAsync(input.VariantId, cancellationToken);
            if (variantType is null)
            {
                return Result.Failure<IReadOnlyList<VariantValue>>(
                    Error.NotFound($"Variant type '{input.VariantId}' not found."));
            }

            var value = variantType.Values.FirstOrDefault(v => v.Id == input.VariantValueId);
            if (value is null)
            {
                return Result.Failure<IReadOnlyList<VariantValue>>(
                    Error.NotFound(
                        $"Variant value '{input.VariantValueId}' not found for type '{input.VariantId}'."));
            }

            resolved.Add(new VariantValue(
                new Variant(
                    variantType.Id,
                    variantType.Name,
                    variantType.SelectionStyle,
                    variantType.Slicer),
                value.Id,
                value.Label,
                value.Key.Value));
        }

        return Result.Success<IReadOnlyList<VariantValue>>(resolved);
    }

    internal static async Task<Result<IReadOnlyList<AttributeValue>>> ResolveAttributeValuesAsync(
        IAttributeRepository attributes,
        IReadOnlyList<AttributeValueInput>? inputs,
        CancellationToken cancellationToken)
    {
        if (inputs is null || inputs.Count == 0)
        {
            return Result.Success<IReadOnlyList<AttributeValue>>([]);
        }

        var resolved = new List<AttributeValue>();
        foreach (var input in inputs)
        {
            var attribute = await attributes.GetByIdAsync(input.AttributeId, cancellationToken);
            if (attribute is null)
            {
                return Result.Failure<IReadOnlyList<AttributeValue>>(
                    Error.NotFound($"Attribute '{input.AttributeId}' not found."));
            }

            var value = attribute.Values.FirstOrDefault(v => v.Id == input.AttributeValueId);
            if (value is null)
            {
                return Result.Failure<IReadOnlyList<AttributeValue>>(
                    Error.NotFound(
                        $"Attribute value '{input.AttributeValueId}' not found for attribute '{input.AttributeId}'."));
            }

            resolved.Add(new ProductAttributeValue(
                new ProductAttribute(attribute.Id, attribute.Key.Value, attribute.Name),
                value.Id,
                value.Name));
        }

        return Result.Success<IReadOnlyList<AttributeValue>>(resolved);
    }

    /// <summary>
    /// Kategoride zorunlu olarak işaretlenen her özniteliğin, ürün düzeyindeki özellik değeri
    /// girdilerinde karşılığının bulunduğunu doğrular (AttributeId üzerinden eşleşir).
    /// </summary>
    /// <param name="attributes">Hata mesajında öznitelik adını çözmek için kullanılan depo.</param>
    /// <param name="category">Zorunluluk atamalarıyla birlikte yüklenmiş kategori.</param>
    /// <param name="providedAttributeIds">Ürün düzeyinde değer verilen öznitelik tanımlayıcıları.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    internal static async Task<Result> EnsureRequiredCategoryAttributesAsync(
        IAttributeRepository attributes,
        Category category,
        IReadOnlySet<Guid> providedAttributeIds,
        CancellationToken cancellationToken)
    {
        foreach (var assignment in category.Assignments.Where(assignment => assignment.Required))
        {
            if (providedAttributeIds.Contains(assignment.AttributeId))
            {
                continue;
            }

            var attribute = await attributes.GetByIdAsync(assignment.AttributeId, cancellationToken);
            var attributeName = attribute?.Name ?? assignment.AttributeId.ToString();
            return Result.Failure(Error.Validation($"Required attribute missing: {attributeName}"));
        }

        return Result.Success();
    }

    internal static async Task<Result> EnsurePlanIsUniqueAsync(
        IProductRepository products,
        ProductCreatePlan plan,
        CancellationToken cancellationToken)
    {
        if (await products.ModelCodeExistsAsync(plan.ModelCode, cancellationToken))
        {
            return Result.Failure(Error.Conflict($"Model code '{plan.ModelCode}' already exists."));
        }

        foreach (var item in plan.Items)
        {
            if (await products.BarcodeExistsAsync(item.Barcode, cancellationToken))
            {
                return Result.Failure(Error.Conflict($"Barcode '{item.Barcode}' already exists."));
            }

            if (!string.IsNullOrWhiteSpace(item.Sku) &&
                await products.VariantSkuExistsAsync(item.Sku, cancellationToken))
            {
                return Result.Failure(Error.Conflict($"Variant SKU '{item.Sku}' already exists."));
            }
        }

        return Result.Success();
    }
}
