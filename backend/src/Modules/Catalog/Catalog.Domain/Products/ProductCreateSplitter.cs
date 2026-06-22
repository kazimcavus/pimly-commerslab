using SharedKernel;

namespace Catalog.Domain.Products;

/// <summary>Slicer eksenine göre bölünmüş tek ürün oluşturma planı.</summary>
/// <example>ModelCode "GOMlek-001-KIRMIZI", Name "Pamuklu Gömlek - Kırmızı", sadece Beden ekseni ve ilgili kalemler.</example>
public sealed record ProductCreatePlan(
    string ModelCode,
    string Name,
    IReadOnlyList<Variant> Variants,
    IReadOnlyList<ProductItemDraft> Items);

/// <summary>Slicer varyant türüne göre ürün oluşturma planlarını üretir.</summary>
/// <example>
/// Renk slicer ise "GOMlek-001" girdisi "GOMlek-001-KIRMIZI" ve "GOMlek-001-MAVI"
/// olmak üzere iki ayrı ürün planına bölünür.
/// </example>
public static class ProductCreateSplitter
{
    public static Result<IReadOnlyList<ProductCreatePlan>> Split(
        string baseModelCode,
        string baseName,
        IReadOnlyList<Variant> variants,
        IReadOnlyList<ProductItemDraft> items)
    {
        var slicerTypes = variants.Where(type => type.Slicer).ToList();
        if (slicerTypes.Count == 0)
        {
            return Result.Success<IReadOnlyList<ProductCreatePlan>>([
                new ProductCreatePlan(baseModelCode, baseName.Trim(), variants, items)
            ]);
        }

        if (slicerTypes.Count > 1)
        {
            return Result.Failure<IReadOnlyList<ProductCreatePlan>>(
                Error.Validation("Only one slicer variant type is allowed per product."));
        }

        if (items.Count == 0)
        {
            return Result.Failure<IReadOnlyList<ProductCreatePlan>>(
                Error.Validation("At least one item is required."));
        }

        var slicerType = slicerTypes[0];
        var remainingTypes = variants.Where(type => !type.Slicer).ToList();
        var stripSlicerFromItems = remainingTypes.Count > 0;
        var groups = new Dictionary<Guid, SlicerGroup>();

        foreach (var item in items)
        {
            var slicerSelection = item.VariantValues?.FirstOrDefault(
                selection => selection.Variant.Id == slicerType.Id);
            if (slicerSelection is null)
            {
                return Result.Failure<IReadOnlyList<ProductCreatePlan>>(
                    Error.Validation($"Each item must include a selection for slicer type '{slicerType.Name}'."));
            }

            if (!groups.TryGetValue(slicerSelection.Id, out var group))
            {
                group = new SlicerGroup(slicerSelection, []);
                groups[slicerSelection.Id] = group;
            }

            group.Items.Add(stripSlicerFromItems
                ? WithoutSlicerSelection(item, slicerType.Id)
                : item);
        }

        var productVariants = stripSlicerFromItems
            ? remainingTypes
            : new List<Variant> { slicerType };

        var usedModelCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var plans = new List<ProductCreatePlan>();
        foreach (var group in groups.Values.OrderBy(g => g.Selection.Name, StringComparer.OrdinalIgnoreCase))
        {
            var modelCodeResult = BuildSplitModelCode(baseModelCode, group.Selection, usedModelCodes);
            if (modelCodeResult.IsFailure)
            {
                return Result.Failure<IReadOnlyList<ProductCreatePlan>>(modelCodeResult.Error);
            }

            plans.Add(new ProductCreatePlan(
                modelCodeResult.Value,
                $"{baseName.Trim()} - {group.Selection.Name}",
                productVariants,
                group.Items));
        }

        return Result.Success<IReadOnlyList<ProductCreatePlan>>(plans);
    }

    private static Result<string> BuildSplitModelCode(
        string baseModelCode,
        VariantValue slicerSelection,
        HashSet<string> usedModelCodes)
    {
        var candidates = new[]
        {
            AppendModelCodeSuffix(baseModelCode, Slugify(slicerSelection.Name)),
            AppendModelCodeSuffix(baseModelCode, slicerSelection.Id.ToString("N")[..8]),
        };

        foreach (var candidate in candidates.Where(candidate => !string.IsNullOrWhiteSpace(candidate)))
        {
            if (usedModelCodes.Add(candidate))
            {
                return Result.Success(candidate);
            }
        }

        return Result.Failure<string>(
            Error.Validation($"Could not allocate a unique model code for slicer value '{slicerSelection.Name}'."));
    }

    private static string AppendModelCodeSuffix(string baseModelCode, string suffix) =>
        string.IsNullOrWhiteSpace(suffix) ? baseModelCode : $"{baseModelCode}-{suffix}";

    private static string Slugify(string value)
    {
        var chars = value.Trim().ToLowerInvariant()
            .Where(ch => char.IsLetterOrDigit(ch))
            .ToArray();

        return chars.Length == 0 ? string.Empty : new string(chars);
    }

    private static ProductItemDraft WithoutSlicerSelection(ProductItemDraft item, Guid slicerTypeId)
    {
        var selections = item.VariantValues?
            .Where(selection => selection.Variant.Id != slicerTypeId)
            .ToList();

        return item with { VariantValues = selections };
    }

    private sealed class SlicerGroup(VariantValue selection, List<ProductItemDraft> items)
    {
        public VariantValue Selection { get; } = selection;

        public List<ProductItemDraft> Items { get; } = items;
    }
}
