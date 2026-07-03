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
        IReadOnlyList<Variant> variants)
        : base(id)
    {
        GroupId = groupId;
        CategoryId = categoryId;
        ModelCode = modelCode;
        Name = name;
        Status = status;
        AttributeValues = attributeValues;
        _variants.AddRange(variants);
    }

    /// <summary>Gets aynı ürün grubundaki kayıtları ilişkilendiren tanımlayıcı.</summary>
    /// <example>Slicer ile bölünen "Gömlek - Kırmızı" ve "Gömlek - Mavi" ürünleri aynı GroupId'yi paylaşır.</example>
    public Guid GroupId { get; private set; }

    /// <summary>Gets ürünün bağlı olduğu katalog kategorisi.</summary>
    /// <example>Tişört kategorisinin Guid'i.</example>
    public Guid CategoryId { get; private set; }

    /// <summary>Gets ürün model kodu.</summary>
    /// <example>GOMlek-001.</example>
    public ModelCode ModelCode { get; private set; } = null!;

    /// <summary>Gets ürün adı.</summary>
    /// <example>Pamuklu Gömlek.</example>
    public string Name { get; private set; } = string.Empty;

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
    public static Result<Product> Create(
        Guid groupId,
        Guid categoryId,
        string modelCode,
        string name,
        ProductStatus status,
        IReadOnlyList<AttributeValue>? attributeValues,
        IReadOnlyList<Variant>? variants,
        IReadOnlyList<ProductItemDraft> items)
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
            snapshots);

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
    public Result UpdateDetails(
        Guid categoryId,
        string name,
        ProductStatus status,
        IReadOnlyList<AttributeValue>? attributeValues)
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

        return item.Update(update);
    }

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
