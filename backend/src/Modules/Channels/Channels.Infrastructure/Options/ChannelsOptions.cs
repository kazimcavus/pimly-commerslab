namespace Channels.Infrastructure.Options;

/// <summary>Channels modülü yapılandırma seçenekleri.</summary>
public sealed class ChannelsOptions
{
    public const string SectionName = "Channels";

    /// <summary>Gets worker poll aralığı (saniye).</summary>
    public int WorkerPollIntervalSeconds { get; init; } = 5;

    /// <summary>Gets a value indicating whether geliştirme ortamında stub taxonomy client kullanılacağı.</summary>
    public bool UseStubTaxonomyClient { get; init; } = true;

    /// <summary>Gets Trendyol API base URL.</summary>
    public string TrendyolApiBaseUrl { get; init; } = "https://api.trendyol.com";
}
