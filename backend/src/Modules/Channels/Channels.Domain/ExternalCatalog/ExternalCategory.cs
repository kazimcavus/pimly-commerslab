using SharedKernel;

namespace Channels.Domain.ExternalCatalog;

/// <summary>Pazaryerinden cache'lenen harici kategori kaydı.</summary>
public sealed class ExternalCategory : Entity<Guid>
{
    private ExternalCategory()
    {
        Marketplace = null!;
    }

    private ExternalCategory(
        Guid id,
        Marketplace marketplace,
        string externalId,
        string name,
        string? parentExternalId,
        string path,
        bool isLeaf,
        DateTimeOffset syncedAt)
        : base(id)
    {
        Marketplace = marketplace;
        ExternalId = externalId;
        Name = name;
        ParentExternalId = parentExternalId;
        Path = path;
        IsLeaf = isLeaf;
        SyncedAt = syncedAt;
    }

    /// <summary>Gets pazaryeri anahtarı.</summary>
    public Marketplace Marketplace { get; private set; }

    /// <summary>Gets pazaryerindeki kategori kimliği.</summary>
    public string ExternalId { get; private set; } = string.Empty;

    /// <summary>Gets kategori adı.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets üst kategori harici kimliği.</summary>
    public string? ParentExternalId { get; private set; }

    /// <summary>Gets breadcrumb yolu.</summary>
    public string Path { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether yaprak kategori olup olmadığı.</summary>
    public bool IsLeaf { get; private set; }

    /// <summary>Gets son sync zamanı.</summary>
    public DateTimeOffset SyncedAt { get; private set; }

    /// <summary>Yeni cache kaydı oluşturur.</summary>
    public static Result<ExternalCategory> Create(
        Marketplace marketplace,
        string externalId,
        string name,
        string? parentExternalId,
        string path,
        bool isLeaf,
        DateTimeOffset syncedAt)
    {
        if (string.IsNullOrWhiteSpace(externalId))
        {
            return Result.Failure<ExternalCategory>(Error.Validation("External category id is required."));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<ExternalCategory>(Error.Validation("External category name is required."));
        }

        return Result.Success(new ExternalCategory(
            Guid.NewGuid(),
            marketplace,
            externalId.Trim(),
            name.Trim(),
            NormalizeOptional(parentExternalId),
            path.Trim(),
            isLeaf,
            syncedAt));
    }

    /// <summary>Mevcut cache kaydını günceller.</summary>
    public void Update(
        string name,
        string? parentExternalId,
        string path,
        bool isLeaf,
        DateTimeOffset syncedAt)
    {
        Name = name.Trim();
        ParentExternalId = NormalizeOptional(parentExternalId);
        Path = path.Trim();
        IsLeaf = isLeaf;
        SyncedAt = syncedAt;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
