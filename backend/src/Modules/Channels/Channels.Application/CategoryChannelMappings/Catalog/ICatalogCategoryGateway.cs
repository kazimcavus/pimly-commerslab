namespace Channels.Application.CategoryChannelMappings.Catalog;

/// <summary>Catalog modülünden kategori bilgisi okuma portu.</summary>
public interface ICatalogCategoryGateway
{
    Task<CatalogCategorySnapshot?> GetByIdAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);
}

/// <summary>Catalog kategori özeti.</summary>
public sealed record CatalogCategorySnapshot(
    Guid Id,
    string Name,
    string? Code);
