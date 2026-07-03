namespace Media.Application.Options;

/// <summary>Media modülü yapılandırma seçenekleri.</summary>
public sealed class MediaOptions
{
    /// <summary>Configuration section adı.</summary>
    public const string SectionName = "Media";

    /// <summary>Gets or sets yerel blob depolama kök dizini.</summary>
    public string StoragePath { get; set; } = "./storage/media";

    /// <summary>Gets or sets dışarıya dönen URL öneki; boşsa relative path kullanılır.</summary>
    public string PublicBaseUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets catalog tarafında kabul edilen görsel URL öneki.</summary>
    public string AllowedUrlPrefix { get; set; } = "/media/";
}
