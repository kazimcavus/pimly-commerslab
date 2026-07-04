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
        // SKU kararı sona bırakılır: önce satırlar toplanır, slicer değeri başına stok kodu
        // dağılımına bakılarak kodun renk-düzeyi mi kalem-düzeyi mi olduğu anlaşılır.
        var slicerAxis = axes.FirstOrDefault(axis => axis.Slicer);
        var pendingItems = new List<PendingItem>();
        foreach (var row in uniqueRows)
        {
            var selections = new List<PlannedVariantSelection>();
            var missingAxis = false;
            string? slicerValueName = null;

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

                if (slicerAxis is not null && string.Equals(axis.ExternalAttributeId, slicerAxis.ExternalAttributeId, StringComparison.Ordinal))
                {
                    slicerValueName = valueName;
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
            pendingItems.Add(new PendingItem(row, selections, itemAttributes, slicerValueName, stockCode));
        }

        if (pendingItems.Count == 0)
        {
            return ProductGroupPlan.Failed(
                productMainId,
                first.Title,
                "Grubun hiçbir satırı içe aktarılamadı (eksen değerleri eksik veya satır yok).");
        }

        // Split planı: slicer değeri başına gerçek stok kodu (tüm kalemleri aynı kodu taşıyorsa)
        // ve o rengin orijinal listeleme başlığı. Kod, renk-düzeyi ise ürünün model kodu olur;
        // kalem SKU'suna yazılmaz (aynı kodu birden çok kaleme yazmak zaten mümkün değil).
        var splits = new List<PlannedSplit>();
        var colorLevelValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (slicerAxis is not null)
        {
            var byValue = pendingItems
                .Where(pending => pending.SlicerValueName is not null)
                .GroupBy(pending => pending.SlicerValueName!, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Aynı stok kodu birden fazla renk grubunda görülüyorsa güvenilmezdir; hiçbirine verilmez.
            var codeOwners = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var valueGroup in byValue)
            {
                var codes = valueGroup.Select(p => p.StockCode).Where(c => c is not null).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (codes.Count == 1)
                {
                    codeOwners[codes[0]!] = codeOwners.TryGetValue(codes[0]!, out var count) ? count + 1 : 1;
                }
            }

            foreach (var valueGroup in byValue)
            {
                var codes = valueGroup.Select(p => p.StockCode).Where(c => c is not null).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                var code = codes.Count == 1 && codeOwners.GetValueOrDefault(codes[0]!) == 1 ? codes[0] : null;
                var title = valueGroup
                    .Select(p => string.IsNullOrWhiteSpace(p.Row.Title) ? null : p.Row.Title.Trim())
                    .FirstOrDefault(t => t is not null);

                if (code is not null)
                {
                    colorLevelValues.Add(valueGroup.Key);
                }

                splits.Add(new PlannedSplit(valueGroup.Key, code, title));
            }
        }

        var items = new List<PlannedItem>();
        foreach (var pending in pendingItems)
        {
            // Renk-düzeyi kod ürünün model kodu olacağı için kalem SKU'su boş kalır;
            // kalem-düzeyi kodlarda eski kural: import genelinde tekilse SKU'ya yazılır.
            var isColorLevelCode = pending.SlicerValueName is not null && colorLevelValues.Contains(pending.SlicerValueName);
            var sku = !isColorLevelCode && pending.StockCode is not null && usedSkus.Add(pending.StockCode)
                ? pending.StockCode
                : null;

            items.Add(new PlannedItem(
                pending.Row.Barcode,
                sku,
                pending.Row.SalePrice,
                pending.Row.ListPrice > pending.Row.SalePrice ? pending.Row.ListPrice : null,
                Math.Max(0, pending.Row.Quantity),
                string.IsNullOrWhiteSpace(pending.Row.CurrencyType) ? null : pending.Row.CurrencyType.Trim(),
                pending.Selections,
                pending.ItemAttributes,
                pending.Row.ImageUrls));
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
            Error: null,
            splits);
    }

    private sealed record PendingItem(
        MarketplaceProductNode Row,
        List<PlannedVariantSelection> Selections,
        List<PlannedAttributeValue> ItemAttributes,
        string? SlicerValueName,
        string? StockCode);

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
/// <remarks>Splits, slicer değeri başına gerçek stok kodu/başlık geçersiz kılmalarıdır.</remarks>
public sealed record ProductGroupPlan(
    string ProductMainId,
    string Name,
    string ExternalCategoryId,
    string ModelCode,
    IReadOnlyList<PlannedVariantAxis> VariantAxes,
    IReadOnlyList<PlannedAttributeValue> AttributeValues,
    IReadOnlyList<PlannedItem> Items,
    IReadOnlyList<string> Warnings,
    string? Error,
    IReadOnlyList<PlannedSplit>? Splits = null)
{
    /// <summary>Gets slicer değeri başına kod/başlık geçersiz kılmaları; boş olabilir.</summary>
    public IReadOnlyList<PlannedSplit> SplitOverrides => Splits ?? [];

    /// <summary>Grubu hata ile işaretleyen kısayol.</summary>
    public static ProductGroupPlan Failed(string productMainId, string name, string error) =>
        new(productMainId, name, string.Empty, productMainId, [], [], [], [], error);
}

/// <summary>Slicer değerine özel plan geçersiz kılması: gerçek stok kodu ve orijinal başlık.</summary>
/// <example>ValueName "Antrasit", StockCode "25CSM02817GR52", Title "Antrasit Klasik Göbekli Halı".</example>
public sealed record PlannedSplit(
    string ValueName,
    string? StockCode,
    string? Title);

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
