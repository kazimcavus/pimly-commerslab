namespace Channels.Application.Imports.Planning;

/// <summary>
/// Pazaryerinden çekilen ürün satırlarını Pimly ürün gruplarına dönüştüren saf planlayıcı.
/// Varyant mı özellik mi kararını kategori attribute tanımlarındaki IsVariant/IsSlicer
/// flag'leriyle verir; hiçbir depo/IO bağımlılığı yoktur.
/// </summary>
/// <remarks>
/// Kurallar:
/// <list type="bullet">
/// <item>Aynı ürünün varyantları ProductMainId ile gruplanır; ModelCode = ProductMainId.</item>
/// <item>IsVariant=true attribute VEYA Renk/color adlı attribute → varyant ekseni; diğerleri ürün düzeyi özellik.</item>
/// <item>Renk/color adlı veya IsSlicer işaretli eksen → renk seçim stili + slicer (varsayılan davranış); kategori "variant" bayrağını taşımasa bile.</item>
/// <item>Tek slicer: birden fazla aday varsa ilki kalır, diğerleri slicer'sız devam eder (uyarı).</item>
/// <item>En fazla 3 eksen: fazlası kalem düzeyi özelliğe indirgenir (uyarı).</item>
/// <item>CompareAtPrice yalnızca ListPrice &gt; SalePrice ise yazılır.</item>
/// </list>
/// </remarks>
public static class ProductImportPlanner
{
    private const int MaxVariantAxes = 3;

    private static readonly string[] ColorNames = ["renk", "color", "colour"];

    /// <summary>Ürün satırlarından import planını üretir.</summary>
    /// <param name="products">Pazaryerinden çekilen tüm ürün satırları.</param>
    /// <param name="attributeDefsByCategory">Dış kategori id → attribute tanımları (cache'ten).</param>
    public static ProductImportPlan BuildPlan(
        IReadOnlyList<MarketplaceProductNode> products,
        IReadOnlyDictionary<string, IReadOnlyList<ProductImportAttributeDef>> attributeDefsByCategory)
    {
        var groups = new List<ProductGroupPlan>();

        // SKU tekilliği tüm import boyunca korunur (DB'de tenant başına tek SKU); bir stok kodu
        // gruplar arasında tekrar ederse yalnızca ilkine atanır, diğerlerinde SKU boş kalır.
        var usedSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var groupRows in products
                     .GroupBy(product => product.ProductMainId, StringComparer.Ordinal)
                     .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            groups.Add(BuildGroup(groupRows.Key, groupRows.ToList(), attributeDefsByCategory, usedSkus));
        }

        return new ProductImportPlan(groups);
    }

    private static ProductGroupPlan BuildGroup(
        string productMainId,
        IReadOnlyList<MarketplaceProductNode> rows,
        IReadOnlyDictionary<string, IReadOnlyList<ProductImportAttributeDef>> attributeDefsByCategory,
        HashSet<string> usedSkus)
    {
        var warnings = new List<string>();
        var first = rows[0];

        if (string.IsNullOrWhiteSpace(first.ExternalCategoryId))
        {
            return ProductGroupPlan.Failed(productMainId, first.Title, "Ürünün pazaryeri kategorisi boş.");
        }

        var externalCategoryId = first.ExternalCategoryId;
        if (rows.Any(row => !string.Equals(row.ExternalCategoryId, externalCategoryId, StringComparison.Ordinal)))
        {
            warnings.Add("Grup içinde farklı kategoriler var; ilk satırın kategorisi kullanıldı.");
        }

        if (!attributeDefsByCategory.TryGetValue(externalCategoryId, out var defs))
        {
            defs = [];
        }

        var defsById = defs.ToDictionary(def => def.ExternalAttributeId, StringComparer.Ordinal);

        // Barkod tekilleştirme: aynı barkod tekrar ederse ilk satır esas alınır.
        var uniqueRows = new List<MarketplaceProductNode>();
        var seenBarcodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            if (seenBarcodes.Add(row.Barcode))
            {
                uniqueRows.Add(row);
            }
            else
            {
                warnings.Add($"Yinelenen barkod atlandı: {row.Barcode}.");
            }
        }

        // Varyant eksen adayları: tanımda IsVariant olan VEYA renk adlı attribute'lar.
        // Trendyol'da renk her zaman slicer'dır; kategori "variant" bayrağını taşımasa bile
        // rengi varsayılan olarak varyant eksenine alırız (kullanıcı sonradan düzenleyebilir).
        var axisCandidates = uniqueRows
            .SelectMany(row => row.Attributes)
            .Where(attribute => defsById.TryGetValue(attribute.ExternalAttributeId, out var def)
                && (def.IsVariant || IsColorName(def.Name)))
            .GroupBy(attribute => attribute.ExternalAttributeId, StringComparer.Ordinal)
            .Select(group =>
            {
                var def = defsById[group.Key];
                var isColor = IsColorName(def.Name);
                return new PlannedVariantAxis(
                    def.ExternalAttributeId,
                    def.Name,
                    isColor,
                    Slicer: def.IsSlicer || isColor);
            })
            .OrderByDescending(axis => axis.Slicer)
            .ThenByDescending(axis => axis.IsColor)
            .ThenBy(axis => axis.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Tek slicer kuralı: ilk slicer kalır, kalanlar slicer'sız devam eder.
        var slicerSeen = false;
        var axes = new List<PlannedVariantAxis>();
        foreach (var axis in axisCandidates)
        {
            if (axis.Slicer && slicerSeen)
            {
                warnings.Add($"'{axis.Name}' ekseni slicer olamadı; ürün başına tek slicer desteklenir.");
                axes.Add(axis with { Slicer = false });
                continue;
            }

            slicerSeen |= axis.Slicer;
            axes.Add(axis);
        }

        // En fazla 3 eksen: fazlası kalem düzeyi özelliğe indirgenir.
        var demotedAxisIds = new HashSet<string>(StringComparer.Ordinal);
        if (axes.Count > MaxVariantAxes)
        {
            foreach (var demoted in axes.Skip(MaxVariantAxes))
            {
                demotedAxisIds.Add(demoted.ExternalAttributeId);
                warnings.Add($"'{demoted.Name}' ekseni özellik olarak içe aktarıldı; en fazla {MaxVariantAxes} varyant ekseni desteklenir.");
            }

            axes = axes.Take(MaxVariantAxes).ToList();
        }

        var axisIds = axes.Select(axis => axis.ExternalAttributeId).ToHashSet(StringComparer.Ordinal);

        // Ürün düzeyi özellikler: ilk satırdaki varyant-olmayan attribute'lar.
        var productAttributes = new List<PlannedAttributeValue>();
        foreach (var attribute in first.Attributes)
        {
            if (axisIds.Contains(attribute.ExternalAttributeId)
                || demotedAxisIds.Contains(attribute.ExternalAttributeId))
            {
                continue;
            }

            var valueName = ResolveValueName(attribute);
            if (valueName is null)
            {
                continue;
            }

            defsById.TryGetValue(attribute.ExternalAttributeId, out var def);
            if (def is null)
            {
                warnings.Add($"'{attribute.Name}' özelliği kategori tanımında yok; yine de özellik olarak aktarıldı.");
            }

            productAttributes.Add(new PlannedAttributeValue(
                attribute.ExternalAttributeId,
                def?.Name ?? attribute.Name,
                valueName,
                attribute.ExternalValueId,
                def?.Required ?? false));
        }

        // Satırlar: eksen seçimleri + indirgenen eksenlerin kalem düzeyi özellik değerleri.
        // SKU tekilliği: Trendyol stockCode çoğu zaman model seviyesindedir (varyantlar aynı
        // kodu paylaşır). SKU kalem başına benzersiz olmak zorunda; çakışan stok kodlarında
        // SKU boş bırakılır (barkod zaten benzersiz tanımlayıcıdır). usedSkus tüm import boyunca paylaşılır.
        var items = new List<PlannedItem>();
        foreach (var row in uniqueRows)
        {
            var selections = new List<PlannedVariantSelection>();
            var missingAxis = false;

            foreach (var axis in axes)
            {
                var attribute = row.Attributes
                    .FirstOrDefault(candidate => string.Equals(
                        candidate.ExternalAttributeId,
                        axis.ExternalAttributeId,
                        StringComparison.Ordinal));

                var valueName = attribute is null ? null : ResolveValueName(attribute);
                if (valueName is null)
                {
                    missingAxis = true;
                    warnings.Add($"Barkod {row.Barcode}: '{axis.Name}' eksen değeri eksik.");
                    break;
                }

                selections.Add(new PlannedVariantSelection(
                    axis.ExternalAttributeId,
                    valueName,
                    attribute!.ExternalValueId));
            }

            if (missingAxis)
            {
                continue;
            }

            var itemAttributes = row.Attributes
                .Where(attribute => demotedAxisIds.Contains(attribute.ExternalAttributeId))
                .Select(attribute =>
                {
                    var valueName = ResolveValueName(attribute);
                    if (valueName is null)
                    {
                        return null;
                    }

                    defsById.TryGetValue(attribute.ExternalAttributeId, out var def);
                    return new PlannedAttributeValue(
                        attribute.ExternalAttributeId,
                        def?.Name ?? attribute.Name,
                        valueName,
                        attribute.ExternalValueId,
                        def?.Required ?? false);
                })
                .Where(planned => planned is not null)
                .Select(planned => planned!)
                .ToList();

            var stockCode = string.IsNullOrWhiteSpace(row.StockCode) ? null : row.StockCode.Trim();
            var sku = stockCode is not null && usedSkus.Add(stockCode) ? stockCode : null;

            items.Add(new PlannedItem(
                row.Barcode,
                sku,
                row.SalePrice,
                row.ListPrice > row.SalePrice ? row.ListPrice : null,
                Math.Max(0, row.Quantity),
                string.IsNullOrWhiteSpace(row.CurrencyType) ? null : row.CurrencyType.Trim(),
                selections,
                itemAttributes,
                row.ImageUrls));
        }

        if (items.Count == 0)
        {
            return ProductGroupPlan.Failed(
                productMainId,
                first.Title,
                "Grubun hiçbir satırı içe aktarılamadı (eksen değerleri eksik veya satır yok).");
        }

        return new ProductGroupPlan(
            productMainId,
            first.Title,
            externalCategoryId,
            ModelCode: productMainId,
            axes,
            productAttributes,
            items,
            warnings,
            Error: null);
    }

    private static bool IsColorName(string name) =>
        ColorNames.Contains(name.Trim().ToLowerInvariant());

    private static string? ResolveValueName(MarketplaceProductAttributeNode attribute)
    {
        if (!string.IsNullOrWhiteSpace(attribute.Value))
        {
            return attribute.Value.Trim();
        }

        if (!string.IsNullOrWhiteSpace(attribute.CustomValue))
        {
            return attribute.CustomValue.Trim();
        }

        return null;
    }
}

/// <summary>Kategori attribute tanımı (cache'ten planlayıcıya taşınan projeksiyon).</summary>
public sealed record ProductImportAttributeDef(
    string ExternalAttributeId,
    string Name,
    bool Required,
    bool AllowCustom,
    bool IsVariant,
    bool IsSlicer);

/// <summary>Planlayıcı çıktısı.</summary>
public sealed record ProductImportPlan(IReadOnlyList<ProductGroupPlan> Groups);

/// <summary>Tek ürün grubunun (productMainId) import planı.</summary>
public sealed record ProductGroupPlan(
    string ProductMainId,
    string Name,
    string ExternalCategoryId,
    string ModelCode,
    IReadOnlyList<PlannedVariantAxis> VariantAxes,
    IReadOnlyList<PlannedAttributeValue> AttributeValues,
    IReadOnlyList<PlannedItem> Items,
    IReadOnlyList<string> Warnings,
    string? Error)
{
    /// <summary>Grubu hata ile işaretleyen kısayol.</summary>
    public static ProductGroupPlan Failed(string productMainId, string name, string error) =>
        new(productMainId, name, string.Empty, productMainId, [], [], [], [], error);
}

/// <summary>Planlanan varyant ekseni.</summary>
public sealed record PlannedVariantAxis(
    string ExternalAttributeId,
    string Name,
    bool IsColor,
    bool Slicer);

/// <summary>Planlanan özellik değeri (ürün veya kalem düzeyi).</summary>
public sealed record PlannedAttributeValue(
    string ExternalAttributeId,
    string AttributeName,
    string ValueName,
    string? ExternalValueId,
    bool Required);

/// <summary>Planlanan satılabilir kalem.</summary>
public sealed record PlannedItem(
    string Barcode,
    string? Sku,
    decimal Price,
    decimal? CompareAtPrice,
    int Stock,
    string? Currency,
    IReadOnlyList<PlannedVariantSelection> VariantSelections,
    IReadOnlyList<PlannedAttributeValue> ItemAttributeValues,
    IReadOnlyList<string> ImageUrls);

/// <summary>Kalemin bir eksendeki seçimi.</summary>
public sealed record PlannedVariantSelection(
    string ExternalAttributeId,
    string ValueName,
    string? ExternalValueId);
