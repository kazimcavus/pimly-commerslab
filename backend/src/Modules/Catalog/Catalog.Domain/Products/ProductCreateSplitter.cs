using SharedKernel;

namespace Catalog.Domain.Products;

/// <summary>Slicer eksenine göre bölünmüş tek ürün oluşturma planı.</summary>
/// <example>ModelCode "GOMlek-001-KIRMIZI", Name "Pamuklu Gömlek - Kırmızı", sadece Beden ekseni ve ilgili kalemler.</example>
/// <remarks>GroupCode her planda paylaşılan temel koddur; SlicerValue bölünen eksen değeridir (bölünmemişse null).</remarks>
public sealed record ProductCreatePlan(
    string ModelCode,
    string Name,
    IReadOnlyList<Variant> Variants,
    IReadOnlyList<ProductItemDraft> Items,
    string? GroupCode = null,
    string? SlicerValue = null,
    string? Description = null);

/// <summary>
/// Slicer değerine özel plan geçersiz kılmaları. Pazaryeri import'unda renk ürününün
/// gerçek stok kodu, orijinal listeleme başlığı ve açıklaması buradan taşınır; verilmeyen
/// alanlar için türetilmiş varsayılanlar (temel kod + slug, "ad - değer") kullanılır.
/// </summary>
/// <example>ValueName "Antrasit", ModelCode "25CSM02817GR52", Name "Antrasit Klasik Göbekli Halı".</example>
public sealed record ProductSplitOverride(
    string ValueName,
    string? ModelCode,
    string? Name,
    string? Description = null);

/// <summary>Slicer varyant türüne göre ürün oluşturma planlarını üretir.</summary>
/// <example>
/// Renk slicer ise "GOMlek-001" girdisi "GOMlek-001-KIRMIZI" ve "GOMlek-001-MAVI"
/// olmak üzere iki ayrı ürün planına bölünür.
/// </example>
public static class ProductCreateSplitter
{
    /// <summary>Slicer eksenine göre ürün oluşturma girdisini bir veya birden fazla plana böler.</summary>
    /// <param name="baseModelCode">Temel model kodu.</param>
    /// <param name="baseName">Temel ürün adı.</param>
    /// <param name="variants">Ürün eksen tanım anlık görüntüleri.</param>
    /// <param name="items">Bölünecek satılabilir kalemler.</param>
    /// <param name="overrides">Slicer değeri başına kod/ad geçersiz kılmaları; opsiyonel.</param>
    public static Result<IReadOnlyList<ProductCreatePlan>> Split(
        string baseModelCode,
        string baseName,
        IReadOnlyList<Variant> variants,
        IReadOnlyList<ProductItemDraft> items,
        IReadOnlyList<ProductSplitOverride>? overrides = null)
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

        var overridesByValue = (overrides ?? [])
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.ValueName))
            .GroupBy(candidate => candidate.ValueName.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var usedModelCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var plans = new List<ProductCreatePlan>();
        foreach (var group in groups.Values.OrderBy(g => g.Selection.Name, StringComparer.OrdinalIgnoreCase))
        {
            overridesByValue.TryGetValue(group.Selection.Name.Trim(), out var groupOverride);

            var modelCodeResult = BuildSplitModelCode(baseModelCode, group.Selection, usedModelCodes, groupOverride?.ModelCode);
            if (modelCodeResult.IsFailure)
            {
                return Result.Failure<IReadOnlyList<ProductCreatePlan>>(modelCodeResult.Error);
            }

            var planName = string.IsNullOrWhiteSpace(groupOverride?.Name)
                ? $"{baseName.Trim()} - {group.Selection.Name}"
                : groupOverride!.Name!.Trim();

            plans.Add(new ProductCreatePlan(
                modelCodeResult.Value,
                planName,
                productVariants,
                group.Items,
                GroupCode: baseModelCode,
                SlicerValue: group.Selection.Name,
                Description: string.IsNullOrWhiteSpace(groupOverride?.Description) ? null : groupOverride!.Description!.Trim()));
        }

        return Result.Success<IReadOnlyList<ProductCreatePlan>>(plans);
    }

    private static Result<string> BuildSplitModelCode(
        string baseModelCode,
        VariantValue slicerSelection,
        HashSet<string> usedModelCodes,
        string? overrideCode)
    {
        // Öncelik: pazaryerinden gelen gerçek kod → temel kod + değer slug'ı → temel kod + kısa id.
        var candidates = new[]
        {
            string.IsNullOrWhiteSpace(overrideCode) ? string.Empty : overrideCode.Trim(),
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
