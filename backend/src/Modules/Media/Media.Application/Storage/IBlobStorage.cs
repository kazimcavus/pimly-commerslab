namespace Media.Application.Storage;

/// <summary>Blob depolama soyutlaması.</summary>
public interface IBlobStorage
{
    /// <summary>İçeriği tenant kapsamında depolar ve meta veriyi döndürür.</summary>
    Task<StoredBlob> SaveAsync(
        Stream content,
        string contentType,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>Depolama anahtarına göre blob'u siler.</summary>
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
