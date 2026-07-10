namespace Channels.Domain.Publications;

/// <summary>Ürün yayın (publish) job'ının yaşam döngüsü durumu.</summary>
public enum PublicationStatus
{
    /// <summary>Kuyruğa alındı, henüz işlenmedi.</summary>
    Pending,

    /// <summary>Worker tarafından işleniyor.</summary>
    Running,

    /// <summary>Tüm kalemler başarıyla yayımlandı.</summary>
    Completed,

    /// <summary>Tamamlandı fakat bazı kalemler hata aldı.</summary>
    CompletedWithErrors,

    /// <summary>Altyapı hatasıyla sonlandı.</summary>
    Failed,
}
