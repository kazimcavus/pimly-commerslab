namespace Channels.Domain.Taxonomy;

/// <summary>Pazaryeri taksonomi sync job durumu.</summary>
public enum TaxonomySyncStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
}
