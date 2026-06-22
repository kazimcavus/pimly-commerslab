using SharedKernel;

namespace Catalog.Domain.Categories;

/// <summary>
/// Bir kategoriye bağlı özniteliğin zorunluluk ve sıralama kurallarını temsil eden varlık.
/// </summary>
public sealed class CategoryAttributeAssignment : Entity<Guid>
{
    private CategoryAttributeAssignment()
    {
    }

    internal CategoryAttributeAssignment(
        Guid id,
        Guid attributeId,
        bool required,
        bool marketplaceRequired,
        int sortOrder)
    {
        Id = id;
        AttributeId = attributeId;
        Required = required;
        MarketplaceRequired = marketplaceRequired;
        SortOrder = sortOrder;
    }

    /// <summary>Gets atanan öznitelik tanımının tanımlayıcısı.</summary>
    public Guid AttributeId { get; private set; }

    /// <summary>Gets a value indicating whether özniteliğin bu kategoride zorunlu olup olmadığı.</summary>
    public bool Required { get; private set; }

    /// <summary>Gets a value indicating whether özniteliğin pazaryerinde zorunlu olup olmadığı.</summary>
    public bool MarketplaceRequired { get; private set; }

    /// <summary>Gets kategori içindeki görüntüleme sırası.</summary>
    public int SortOrder { get; private set; }

    internal void Update(bool required, bool marketplaceRequired, int sortOrder)
    {
        Required = required;
        MarketplaceRequired = marketplaceRequired;
        SortOrder = sortOrder;
    }
}
