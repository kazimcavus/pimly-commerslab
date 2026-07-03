namespace Channels.Application.Options;

/// <summary>Ürün import hattı yapılandırma seçenekleri ("Channels" bölümünden bağlanır).</summary>
public sealed class ProductImportOptions
{
    public const string SectionName = "Channels";

    /// <summary>Gets sayfa başına çekilecek ürün sayısı (Trendyol maks 200).</summary>
    public int ImportPageSize { get; init; } = 200;

    /// <summary>Gets ilerlemenin veritabanına kaç ürün grubunda bir kaydedileceği.</summary>
    public int ImportProgressSaveEveryGroups { get; init; } = 20;

    /// <summary>Gets ürün başına içe aktarılacak en fazla görsel sayısı.</summary>
    public int ImportMaxImagesPerProduct { get; init; } = 8;
}
