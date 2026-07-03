using Media.Application.Storage;

namespace Media.Infrastructure.Storage;

/// <summary>Magic-byte tabanlı görsel MIME tespiti adaptörü.</summary>
internal sealed class ImageContentTypeDetectorAdapter : IImageContentTypeDetector
{
    /// <inheritdoc/>
    public string? Detect(Stream content) => ImageContentTypeDetector.Detect(content);
}
