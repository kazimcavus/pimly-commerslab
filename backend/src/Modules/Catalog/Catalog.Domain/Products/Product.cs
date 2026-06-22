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

    private Product()
    {
    }

    private Product(
        Guid id,
        Guid groupId,
        ModelCode modelCode,
        string name,
        ProductStatus status,
        IReadOnlyList<AttributeValue> attributeValues,
        IReadOnlyList<Variant> variants)
        : base(id)
    {
        GroupId = groupId;
        ModelCode = modelCode;
        Name = name;
        Status = status;
        AttributeValues = attributeValues;
        _variants.AddRange(variants);
    }

    /// <summary>Gets aynı ürün grubundaki kayıtları ilişkilendiren tanımlayıcı.</summary>
    /// <example>Slicer ile bölünen "Gömlek - Kırmızı" ve "Gömlek - Mavi" ürünleri aynı GroupId'yi paylaşır.</example>
    public Guid GroupId { get; private set; }

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

    public static Result<Product> Create(
        Guid groupId,
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

    public Result UpdateDetails(
        string name,
        ProductStatus status,
        IReadOnlyList<AttributeValue>? attributeValues)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("Product name is required."));
        }

        Name = name.Trim();
        Status = status;
        if (attributeValues is not null)
        {
            AttributeValues = attributeValues;
        }

        return Result.Success();
    }

    public Result UpdateItem(Guid itemId, ProductItemUpdate update)
    {
        var item = _items.FirstOrDefault(v => v.Id == itemId);
        if (item is null)
        {
            return Result.Failure(Error.NotFound("Product variant not found."));
        }

        return item.Update(update);
    }

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

    internal void LoadItems(IEnumerable<ProductItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
    }

    internal void LoadVariants(IEnumerable<Variant> variants)
    {
        _variants.Clear();
        _variants.AddRange(variants);
    }

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
