using Catalog.Domain.Products.Events;
using SharedKernel;

namespace Catalog.Domain.Products;

/// <summary>
/// Ürün ve alt satılabilir kalemlerini yöneten kök aggregate.
/// Model kodu, ad, durum, ürün düzeyinde özellik değerleri ve eksen tanımlarını;
/// her satılabilir kombinasyonu <see cref="ProductItem"/> olarak tutar.
/// </summary>
/// <example>
/// ModelCode "GOMlek-001", Name "Pamuklu Gömlek", Renk ve Beden eksenleriyle
/// Kırmızı-S ve Kırmızı-M olmak üzere iki ProductItem.
/// </example>
public sealed class Product : AggregateRoot<Guid>
{
    private readonly List<ProductItem> _items = [];
    private readonly List<Variant> _variants = [];
    private readonly List<ProductImage> _images = [];

    private Product()
    {
    }

    private Product(
        Guid id,
        Guid groupId,
        Guid categoryId,
        ModelCode modelCode,
        string name,
        ProductStatus status,
        IReadOnlyList<AttributeValue> attributeValues,
        IReadOnlyList<Variant> variants,
        string? groupCode,
        string? slicerValue,
        Guid? brandId,
        string? description)
        : base(id)
    {
        GroupId = groupId;
        CategoryId = categoryId;
        ModelCode = modelCode;
        Name = name;
        Status = status;
        AttributeValues = attributeValues;
        GroupCode = groupCode;
        SlicerValue = slicerValue;
        BrandId = brandId;
        Description = description;
        _variants.AddRange(variants);
    }

    /// <summary>Gets aynı ürün grubundaki kayıtları ilişkilendiren tanımlayıcı.</summary>
    /// <example>Slicer ile bölünen "Gömlek - Kırmızı" ve "Gömlek - Mavi" ürünleri aynı GroupId'yi paylaşır.</example>
    public Guid GroupId { get; private set; }

    /// <summary>Gets ürünün bağlı olduğu katalog kategorisi.</summary>
    /// <example>Tişört kategorisinin Guid'i.</example>
    public Guid CategoryId { get; private set; }

    /// <summary>Gets ürünün bağlı olduğu opsiyonel marka; markasız ürün için null.</summary>
    /// <example>Pimly markasının Guid'i.</example>
    public Guid? BrandId { get; private set; }

    /// <summary>Gets ürün model kodu.</summary>
    /// <example>GOMlek-001.</example>
    public ModelCode ModelCode { get; private set; } = null!;

    /// <summary>
    /// Gets grubun insan-okur paylaşılan kodu (pazaryerindeki "model kodu"na karşılık gelir).
    /// Slicer ile bölünen renk ürünlerinin tümü aynı grup kodunu taşır; ModelCode ise renk
    /// ürününe özgüdür (pazaryerindeki "stok kodu").
    /// </summary>
    /// <example>Grup kodu "25CSM02817", renk ürününün model kodu "25CSM02817GR52".</example>
    public string? GroupCode { get; private set; }

    /// <summary>Gets slicer ile bölünmüş ürünün eksen değeri (ör. renk adı); bölünmemişse null.</summary>
    /// <example>Antrasit.</example>
    public string? SlicerValue { get; private set; }

    /// <summary>Gets ürün adı.</summary>
    /// <example>Pamuklu Gömlek.</example>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets ürünün opsiyonel açıklama metni; boş bırakılmışsa null.</summary>
    /// <example>Nefes alan pamuklu kumaştan klasik kesim gömlek.</example>
    public string? Description { get; private set; }

    /// <summary>Gets ürünün yaşam döngüsü durumu.</summary>
    /// <example>Draft.</example>
    public ProductStatus Status { get; private set; }

    /// <summary>Gets ürün düzeyinde özellik değeri anlık görüntüleri.</summary>
    /// <example>"Malzeme" özelliğinde "Pamuk" değeri.</example>
    public IReadOnlyList<AttributeValue> AttributeValues { get; private set; } = [];

    /// <summary>Gets ürün oluşturulurken sabitlenen eksen tanım anlık görüntüleri.</summary>
    /// <example>Renk (Color) ve Beden (List) eksenleri.</example>
    public IReadOnlyList<Variant> Variants => _variants.AsReadOnly();

    /// <summary>Gets ürüne ait satılabilir kalemler.</summary>
    /// <example>Kırmızı-S ve Kırmızı-M barkod/fiyat/stok satırları.</example>
    public IReadOnlyCollection<ProductItem> Items => _items.AsReadOnly();

    /// <summary>Gets ürün galerisi görselleri.</summary>
    public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();

    /// <summary>Yeni ürün aggregate'i oluşturur ve <see cref="ProductCreated"/> alan olayını yayımlar.</summary>
    /// <param name="groupId">Aynı ürün grubundaki kayıtları ilişkilendiren tanımlayıcı.</param>
    /// <param name="categoryId">Ürünün bağlı olduğu katalog kategorisi.</param>
    /// <param name="modelCode">Benzersiz model kodu.</param>
    /// <param name="name">Ürün adı.</param>
    /// <param name="status">Başlangıç yaşam döngüsü durumu.</param>
    /// <param name="attributeValues">Ürün düzeyinde özellik değerleri; opsiyonel.</param>
    /// <param name="variants">Sabitlenen eksen tanım anlık görüntüleri; opsiyonel.</param>
    /// <param name="items">Oluşturulacak satılabilir kalemler.</param>
    /// <param name="groupCode">Grubun paylaşılan insan-okur kodu; opsiyonel.</param>
    /// <param name="slicerValue">Slicer bölmesinden gelen eksen değeri (ör. renk); opsiyonel.</param>
    /// <param name="brandId">Ürünün bağlanacağı opsiyonel marka; markasız için null.</param>
    /// <param name="description">Ürünün opsiyonel açıklama metni; boş bırakılmışsa null.</param>
    public static Result<Product> Create(
        Guid groupId,
        Guid categoryId,
        string modelCode,
        string name,
        ProductStatus status,
        IReadOnlyList<AttributeValue>? attributeValues,
        IReadOnlyList<Variant>? variants,
        IReadOnlyList<ProductItemDraft> items,
        string? groupCode = null,
        string? slicerValue = null,
        Guid? brandId = null,
        string? description = null)
    {
        if (groupId == Guid.Empty)
        {
            return Result.Failure<Product>(Error.Validation("Group id is required."));
        }

        if (categoryId == Guid.Empty)
        {
            return Result.Failure<Product>(Error.Validation("Category id is required."));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Product>(Error.Validation("Product name is required."));
        }

        var modelCodeResult = ModelCode.Create(modelCode);
        if (modelCodeResult.IsFailure)
        {
            return Result.Failure<Product>(modelCodeResult.Error);
        }

        var snapshots = variants ?? [];
        var structureResult = ValidateVariantStructure(snapshots.Count, items.Count);
        if (structureResult.IsFailure)
        {
            return Result.Failure<Product>(structureResult.Error);
        }

        var product = new Product(
            Guid.NewGuid(),
            groupId,
            categoryId,
            modelCodeResult.Value,
            name.Trim(),
            status,
            attributeValues ?? [],
            snapshots,
            string.IsNullOrWhiteSpace(groupCode) ? null : groupCode.Trim(),
            string.IsNullOrWhiteSpace(slicerValue) ? null : slicerValue.Trim(),
            brandId,
            string.IsNullOrWhiteSpace(description) ? null : description.Trim());

        foreach (var draft in items)
        {
            var itemResult = ProductItem.Create(draft);
            if (itemResult.IsFailure)
            {
                return Result.Failure<Product>(itemResult.Error);
            }

            product._items.Add(itemResult.Value);
        }

        product.RaiseDomainEvent(new ProductCreated(product.Id, product.ModelCode.Value));
        return Result.Success(product);
    }

    /// <summary>Ürün adını, durumunu ve opsiyonel özellik değerlerini günceller.</summary>
    /// <param name="categoryId">Yeni katalog kategorisi.</param>
    /// <param name="name">Yeni ürün adı.</param>
    /// <param name="status">Yeni yaşam döngüsü durumu.</param>
    /// <param name="attributeValues">Güncellenecek özellik değerleri; null ise mevcut değerler korunur.</param>
    /// <param name="brandId">Yeni opsiyonel marka; markayı kaldırmak için null.</param>
    /// <param name="description">Yeni opsiyonel açıklama metni; boş bırakılmışsa null olarak kaydedilir.</param>
    public Result UpdateDetails(
        Guid categoryId,
        string name,
        ProductStatus status,
        IReadOnlyList<AttributeValue>? attributeValues,
        Guid? brandId = null,
        string? description = null)
    {
        if (categoryId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("Category id is required."));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("Product name is required."));
        }

        CategoryId = categoryId;
        Name = name.Trim();
        Status = status;
        BrandId = brandId;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (attributeValues is not null)
        {
            AttributeValues = attributeValues;
        }

        return Result.Success();
    }

    /// <summary>Belirtilen satılabilir kalemin fiyat, stok ve özellik bilgilerini günceller.</summary>
    /// <param name="itemId">Güncellenecek kalem tanımlayıcısı.</param>
    /// <param name="update">Güncelleme girdisi.</param>
    public Result UpdateItem(Guid itemId, ProductItemUpdate update)
    {
        var item = _items.FirstOrDefault(v => v.Id == itemId);
        if (item is null)
        {
            return Result.Failure(Error.NotFound("Product variant not found."));
        }

        // Barkod/SKU değişiyorsa ürün içindeki diğer kalemlerle çakışmamalı.
        if (!string.IsNullOrWhiteSpace(update.Barcode)
            && _items.Any(other => other.Id != itemId
                && string.Equals(other.Barcode, update.Barcode.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure(Error.Conflict("Barcode already exists on this product."));
        }

        if (!string.IsNullOrWhiteSpace(update.Sku)
            && _items.Any(other => other.Id != itemId
                && string.Equals(other.Sku, update.Sku.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure(Error.Conflict("Variant SKU already exists on this product."));
        }

        return item.Update(update);
    }

    /// <summary>
    /// Ürüne yeni bir satılabilir kalem ekler. Kalem, üründeki her eksen için tam bir seçim
    /// içermeli; eksen kombinasyonu, barkod ve SKU ürün içinde benzersiz olmalıdır.
    /// </summary>
    /// <param name="draft">Kalem oluşturma girdisi.</param>
    public Result<ProductItem> AddItem(ProductItemDraft draft)
    {
        if (_variants.Count == 0)
        {
            return Result.Failure<ProductItem>(
                Error.Validation("Basic product must have exactly one variant."));
        }

        var selections = draft.VariantValues ?? [];
        var expectedTypeIds = _variants.Select(type => type.Id).ToHashSet();
        var selectionTypeIds = selections.Select(selection => selection.Variant.Id).ToHashSet();
        if (selections.Count != _variants.Count || !expectedTypeIds.SetEquals(selectionTypeIds))
        {
            return Result.Failure<ProductItem>(
                Error.Validation("Item must include exactly one selection for each variant type."));
        }

        var newKey = SelectionKey(selections);
        if (_items.Any(existing => SelectionKey(existing.VariantValues).SequenceEqual(newKey)))
        {
            return Result.Failure<ProductItem>(
                Error.Conflict("An item with the same variant selections already exists."));
        }

        var barcode = draft.Barcode?.Trim();
        if (!string.IsNullOrWhiteSpace(barcode)
            && _items.Any(existing => string.Equals(existing.Barcode, barcode, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure<ProductItem>(Error.Conflict("Barcode already exists on this product."));
        }

        if (!string.IsNullOrWhiteSpace(draft.Sku)
            && _items.Any(existing => string.Equals(existing.Sku, draft.Sku.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure<ProductItem>(Error.Conflict("Variant SKU already exists on this product."));
        }

        var itemResult = ProductItem.Create(draft);
        if (itemResult.IsFailure)
        {
            return itemResult;
        }

        _items.Add(itemResult.Value);
        return Result.Success(itemResult.Value);
    }

    private static List<Guid> SelectionKey(IReadOnlyList<VariantValue>? selections) =>
        (selections ?? []).OrderBy(selection => selection.Variant.Id).Select(selection => selection.Id).ToList();

    /// <summary>Üründen bir satılabilir kalemi kaldırır; en az bir kalem kalmalıdır.</summary>
    /// <param name="itemId">Kaldırılacak kalem tanımlayıcısı.</param>
    public Result RemoveItem(Guid itemId)
    {
        var item = _items.FirstOrDefault(v => v.Id == itemId);
        if (item is null)
        {
            return Result.Failure(Error.NotFound("Product variant not found."));
        }

        if (_variants.Count == 0 && _items.Count <= 1)
        {
            return Result.Failure(Error.Validation("Basic product must have exactly one variant."));
        }

        if (_items.Count <= 1)
        {
            return Result.Failure(Error.Validation("Product must have at least one variant."));
        }

        _items.Remove(item);
        return Result.Success();
    }

    /// <summary>Ürün galerisine yeni görsel ekler; birincil görsel seçilirse diğerleri sıfırlanır.</summary>
    /// <param name="url">Görsel URL'si.</param>
    /// <param name="sortOrder">Galeri sıralaması.</param>
    /// <param name="altText">Erişilebilirlik metni; opsiyonel.</param>
    /// <param name="isPrimary">Kapak görseli olarak işaretlensin mi.</param>
    /// <param name="variantValueId">İlişkili varyant değeri; opsiyonel.</param>
    public Result<ProductImage> AddImage(
        string url,
        int sortOrder,
        string? altText,
        bool isPrimary,
        Guid? variantValueId)
    {
        var createResult = ProductImage.Create(url, sortOrder, altText, isPrimary, variantValueId);
        if (createResult.IsFailure)
        {
            return createResult;
        }

        if (isPrimary)
        {
            ClearPrimaryImage();
        }

        _images.Add(createResult.Value);
        return Result.Success(createResult.Value);
    }

    /// <summary>Mevcut galeri görselinin bilgilerini günceller.</summary>
    /// <param name="imageId">Güncellenecek görsel tanımlayıcısı.</param>
    /// <param name="url">Yeni görsel URL'si.</param>
    /// <param name="sortOrder">Yeni galeri sıralaması.</param>
    /// <param name="altText">Yeni erişilebilirlik metni; opsiyonel.</param>
    /// <param name="isPrimary">Kapak görseli olarak işaretlensin mi.</param>
    /// <param name="variantValueId">İlişkili varyant değeri; opsiyonel.</param>
    public Result UpdateImage(
        Guid imageId,
        string url,
        int sortOrder,
        string? altText,
        bool isPrimary,
        Guid? variantValueId)
    {
        var image = _images.FirstOrDefault(i => i.Id == imageId);
        if (image is null)
        {
            return Result.Failure(Error.NotFound("Product image not found."));
        }

        if (isPrimary)
        {
            foreach (var other in _images.Where(i => i.Id != imageId && i.IsPrimary))
            {
                other.MarkPrimary(false);
            }
        }

        return image.Update(url, sortOrder, altText, isPrimary, variantValueId);
    }

    /// <summary>Ürün galerisinden bir görseli kaldırır.</summary>
    /// <param name="imageId">Kaldırılacak görsel tanımlayıcısı.</param>
    public Result RemoveImage(Guid imageId)
    {
        var image = _images.FirstOrDefault(i => i.Id == imageId);
        if (image is null)
        {
            return Result.Failure(Error.NotFound("Product image not found."));
        }

        _images.Remove(image);
        return Result.Success();
    }

    /// <summary>Belirtilen görseli kapak görseli yapar; diğer birincil işaretleri kaldırır.</summary>
    /// <param name="imageId">Birincil yapılacak görsel tanımlayıcısı.</param>
    public Result SetPrimaryImage(Guid imageId)
    {
        var image = _images.FirstOrDefault(i => i.Id == imageId);
        if (image is null)
        {
            return Result.Failure(Error.NotFound("Product image not found."));
        }

        ClearPrimaryImage();
        image.MarkPrimary(true);
        return Result.Success();
    }

    /// <summary>Kalıcı depodan yüklenen satılabilir kalemleri aggregate'e bağlar.</summary>
    /// <param name="items">Ürüne ait kalem koleksiyonu.</param>
    internal void LoadItems(IEnumerable<ProductItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
    }

    /// <summary>Kalıcı depodan yüklenen eksen tanımlarını aggregate'e bağlar.</summary>
    /// <param name="variants">Ürüne ait eksen anlık görüntüleri.</param>
    internal void LoadVariants(IEnumerable<Variant> variants)
    {
        _variants.Clear();
        _variants.AddRange(variants);
    }

    /// <summary>Kalıcı depodan yüklenen galeri görsellerini aggregate'e bağlar.</summary>
    /// <param name="images">Ürüne ait görsel koleksiyonu.</param>
    internal void LoadImages(IEnumerable<ProductImage> images)
    {
        _images.Clear();
        _images.AddRange(images);
    }

    private void ClearPrimaryImage()
    {
        foreach (var image in _images.Where(i => i.IsPrimary))
        {
            image.MarkPrimary(false);
        }
    }

    /// <summary>Eksen sayısı ile kalem sayısının ürün tipine uygun olup olmadığını doğrular.</summary>
    /// <param name="variantCount">Eksen tanımı sayısı; 0 ile 3 arasında olmalıdır.</param>
    /// <param name="itemCount">Satılabilir kalem sayısı.</param>
    internal static Result ValidateVariantStructure(int variantCount, int itemCount)
    {
        if (variantCount is < 0 or > 3)
        {
            return Result.Failure(Error.Validation("Product must have between 0 and 3 variant types."));
        }

        if (variantCount == 0)
        {
            return itemCount == 1
                ? Result.Success()
                : Result.Failure(Error.Validation("Basic product must have exactly one variant."));
        }

        return itemCount >= 1
            ? Result.Success()
            : Result.Failure(Error.Validation("Variant product must have at least one variant."));
    }
}
