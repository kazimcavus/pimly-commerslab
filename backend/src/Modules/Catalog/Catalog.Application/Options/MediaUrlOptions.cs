namespace Catalog.Application.Options;

/// <summary>Catalog tarafında kabul edilen media URL yapılandırması.</summary>
public sealed class MediaUrlOptions
{
    /// <summary>Configuration section adı (Media modülü ile paylaşılır).</summary>
    public const string SectionName = "Media";

    /// <summary>Gets or sets kabul edilen görsel URL öneki.</summary>
    public string AllowedUrlPrefix { get; set; } = "/media/";
}
