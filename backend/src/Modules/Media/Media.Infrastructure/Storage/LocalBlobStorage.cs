using Media.Application.Options;
using Media.Application.Storage;
using Microsoft.Extensions.Options;

namespace Media.Infrastructure.Storage;

/// <summary>Yerel dosya sistemi üzerinde blob depolama.</summary>
internal sealed class LocalBlobStorage(IOptions<MediaOptions> options) : IBlobStorage
{
    /// <inheritdoc/>
    public async Task<StoredBlob> SaveAsync(
        Stream content,
        string contentType,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var storageKey = BuildStorageKey(contentType, tenantId);
        var absolutePath = ResolveAbsolutePath(storageKey);
        var directory = Path.GetDirectoryName(absolutePath)
            ?? throw new InvalidOperationException("Invalid storage path.");

        Directory.CreateDirectory(directory);

        await using var fileStream = new FileStream(
            absolutePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await content.CopyToAsync(fileStream, cancellationToken);
        var sizeBytes = fileStream.Length;

        return new StoredBlob(storageKey, contentType, sizeBytes);
    }

    /// <inheritdoc/>
    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var absolutePath = ResolveAbsolutePath(storageKey);
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }

        return Task.CompletedTask;
    }

    internal string ResolveAbsolutePath(string storageKey) =>
        Path.GetFullPath(Path.Combine(options.Value.StoragePath, storageKey.Replace('/', Path.DirectorySeparatorChar)));

    private static string BuildStorageKey(string contentType, Guid tenantId)
    {
        var id = Guid.NewGuid();
        var shardA = id.ToString("N")[..2];
        var shardB = id.ToString("N")[2..4];
        var extension = contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => throw new InvalidOperationException($"Unsupported content type: {contentType}"),
        };

        return $"{tenantId:N}/{shardA}/{shardB}/{id:N}{extension}";
    }
}
