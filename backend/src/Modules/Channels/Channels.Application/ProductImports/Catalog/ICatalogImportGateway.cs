using SharedKernel;

namespace Channels.Application.ProductImports.Catalog;

/// <summary>
/// Ürün import hattının Catalog modülüne yazma kapısı. Mevcut okuma gateway'leri gibi
/// host kompozisyonunda (worker) Catalog handler/repolarına delege edilerek uygulanır;
/// Channels modülü Catalog tiplerine doğrudan bağımlanmaz.
/// Tüm işlemler idempotenttir: var olan kayıt yeniden kullanılır.
/// </summary>
public interface ICatalogImportGateway
{
    /// <summary>Kategori yolunu (kökten yaprağa) garanti eder ve yaprağın kimliğini döndürür.</summary>
    /// <param name="pathSegments">Ör. ["Moda", "Erkek", "Gömlek"] veya düz model için tek eleman ["Gömlek"].</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    Task<Result<Guid>> EnsureCategoryPathAsync(
        IReadOnlyList<string> pathSegments,
        CancellationToken cancellationToken = default);

    /// <summary>Kategorinin hâlâ var olup olmadığını döner (eşleme dedup doğrulaması için).</summary>
    Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>Özelliği ada göre garanti eder (anahtar addan türetilir).</summary>
    Task<Result<Guid>> EnsureAttributeAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Markayı ada göre garanti eder (tenant içinde idempotent). Varsa mevcut markanın
    /// kimliğini döndürür; yoksa <paramref name="externalId"/> kodunu taşıyan yeni marka oluşturur.
    /// </summary>
    /// <param name="name">Marka adı.</param>
    /// <param name="externalId">Opsiyonel harici marka kimliği (ör. Trendyol brandId); marka koduna yazılır.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    Task<Result<Guid>> EnsureBrandAsync(string name, string? externalId, CancellationToken cancellationToken = default);

    /// <summary>Özellik değerini garanti eder.</summary>
    Task<Result<Guid>> EnsureAttributeValueAsync(
        Guid attributeId,
        string valueName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Varyant eksenini garanti eder. Slicer istenip başka bir slicer ekseni zaten varsa
    /// eksen slicer'sız oluşturulur ve <c>SlicerDemoted</c> true döner (tek slicer kuralı).
    /// </summary>
    Task<Result<EnsuredVariantSnapshot>> EnsureVariantAsync(
        string name,
        bool isColor,
        bool slicer,
        CancellationToken cancellationToken = default);

    /// <summary>Varyant değerini garanti eder.</summary>
    Task<Result<Guid>> EnsureVariantValueAsync(
        Guid variantId,
        string label,
        CancellationToken cancellationToken = default);

    /// <summary>Özelliği kategoriye atar; zaten atanmışsa başarı döner.</summary>
    Task<Result> AssignAttributeToCategoryAsync(
        Guid categoryId,
        Guid attributeId,
        bool required,
        int sortOrder,
        CancellationToken cancellationToken = default);

    /// <summary>Model kodu veya barkodlardan biri zaten kayıtlıysa true döner (grup atlanır).</summary>
    Task<bool> ProductGroupExistsAsync(
        string modelCode,
        IReadOnlyList<string> barcodes,
        CancellationToken cancellationToken = default);

    /// <summary>Ürün grubunu oluşturur (slicer ekseni Catalog tarafında ürünleri böler).</summary>
    Task<Result<IReadOnlyList<CreatedProductSnapshot>>> CreateProductsBatchAsync(
        CatalogProductBatchInput input,
        CancellationToken cancellationToken = default);

    /// <summary>Harici görseli indirip medya deposuna alır ve ürüne ekler.</summary>
    Task<Result> AddProductImageAsync(
        Guid productId,
        string sourceUrl,
        int sortOrder,
        bool isPrimary,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fiyat tanımını ada göre garanti eder (tenant içinde idempotent). Varsa mevcut tanımın
    /// kimliğini döndürür; yoksa <paramref name="code"/> makine kodunu taşıyan yeni tanım oluşturur.
    /// </summary>
    /// <param name="name">Fiyat tanımı adı (ör. "TY Satış").</param>
    /// <param name="code">Opsiyonel makine kodu (ör. "ty_sale").</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    Task<Result<Guid>> EnsurePriceDefinitionAsync(
        string name,
        string? code,
        CancellationToken cancellationToken = default);

    /// <summary>Kalemin belirtilen fiyat tanımındaki tutarını yazar (upsert).</summary>
    Task<Result> UpsertItemPriceAsync(
        Guid productItemId,
        Guid priceDefinitionId,
        decimal amount,
        string? currency,
        CancellationToken cancellationToken = default);
}

/// <summary>Garanti edilen varyant ekseninin anlık görüntüsü.</summary>
public sealed record EnsuredVariantSnapshot(Guid Id, string Name, bool IsColor, bool Slicer, bool SlicerDemoted);

/// <summary>Ürün grubu oluşturma girdisi (modül-bağımsız).</summary>
/// <remarks>Splits, slicer değeri başına gerçek stok kodu ve orijinal listeleme başlığını taşır.</remarks>
public sealed record CatalogProductBatchInput(
    Guid GroupId,
    Guid CategoryId,
    string ModelCode,
    string Name,
    string Status,
    IReadOnlyList<CatalogSelectionInput> AttributeValues,
    IReadOnlyList<CatalogVariantAxisInput> Variants,
    IReadOnlyList<CatalogProductItemInput> Items,
    IReadOnlyList<CatalogSplitInput>? Splits = null,
    Guid? BrandId = null);

/// <summary>Slicer değerine özel ürün geçersiz kılmaları (kod/ad).</summary>
/// <example>ValueName "Antrasit", ModelCode "25CSM02817GR52", Name "Antrasit Klasik Göbekli Halı".</example>
public sealed record CatalogSplitInput(
    string ValueName,
    string? ModelCode,
    string? Name);

/// <summary>Ürünün kullandığı varyant ekseni.</summary>
public sealed record CatalogVariantAxisInput(Guid VariantId, bool IsColor, bool Slicer);

/// <summary>Özellik veya varyant değeri seçimi (kimlik çifti).</summary>
public sealed record CatalogSelectionInput(Guid Id, Guid ValueId);

/// <summary>Satılabilir kalem girdisi.</summary>
public sealed record CatalogProductItemInput(
    string? Sku,
    string Barcode,
    decimal Price,
    decimal? CompareAtPrice,
    int Stock,
    IReadOnlyList<CatalogSelectionInput> VariantValues,
    IReadOnlyList<CatalogSelectionInput> AttributeValues);

/// <summary>Oluşturulan ürünün anlık görüntüsü; kalem kimlikleri barkod ile eşlidir.</summary>
public sealed record CreatedProductSnapshot(
    Guid ProductId,
    IReadOnlyDictionary<string, Guid> ItemIdByBarcode);
