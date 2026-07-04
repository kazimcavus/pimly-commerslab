namespace Channels.Domain.Imports;

/// <summary>Pazaryeri ürün import job durumu.</summary>
public enum ProductImportStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    CompletedWithErrors = 3,
    Failed = 4,
    Cancelled = 5,
}
