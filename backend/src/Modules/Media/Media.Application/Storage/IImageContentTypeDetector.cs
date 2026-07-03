namespace Media.Application.Storage;

/// <summary>Magic-byte tabanlı görsel MIME tespiti.</summary>
public interface IImageContentTypeDetector
{
    /// <summary>Desteklenen görsel MIME tipini tespit eder; geçersizse null döner.</summary>
    string? Detect(Stream content);
}
