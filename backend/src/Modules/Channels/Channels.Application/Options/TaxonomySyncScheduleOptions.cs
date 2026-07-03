namespace Channels.Application.Options;

/// <summary>Günde birkaç kez çalışan taxonomy sync zamanlaması.</summary>
public sealed class TaxonomySyncScheduleOptions
{
    public const string SectionName = "Channels:TaxonomySyncSchedule";

    /// <summary>Gets a value indicating whether zamanlanmış taxonomy sync'in etkin olup olmadığı.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Gets zaman dilimi kontrol aralığı (saniye).</summary>
    public int CheckIntervalSeconds { get; init; } = 60;

    /// <summary>
    /// Gets UTC saat dilimlerinin başlangıç saatleri (HH:mm).
    /// Varsayılan: gün 3 eş parçaya bölünür — 00:00, 08:00, 16:00 UTC.
    /// </summary>
    public IReadOnlyList<string> TimesUtc { get; init; } = ["00:00", "08:00", "16:00"];
}
