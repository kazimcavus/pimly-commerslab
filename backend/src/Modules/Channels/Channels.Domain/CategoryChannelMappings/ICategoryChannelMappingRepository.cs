using Channels.Domain.Marketplaces;
using SharedKernel;

namespace Channels.Domain.CategoryChannelMappings;

/// <summary>CategoryChannelMapping aggregate depo arabirimi.</summary>
public interface ICategoryChannelMappingRepository
{
    Task<CategoryChannelMapping?> GetAsync(
        Marketplace marketplace,
        Guid catalogCategoryId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryChannelMapping>> ListAsync(
        Marketplace marketplace,
        Guid? catalogCategoryId,
        Pagination pagination,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Marketplace marketplace,
        Guid? catalogCategoryId,
        CancellationToken cancellationToken = default);

    Task<string?> ResolveExternalIdAsync(
        Marketplace marketplace,
        Guid catalogCategoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dış kategori kimliğine eşlenmiş mevcut mapping'i getirir (ters çözümleme).
    /// Import dedup'ı bunu kullanır: aynı dış kategori tekrar görüldüğünde, ağaçtaki
    /// konumundan bağımsız olarak daha önce eşlenen catalog kategorisi yeniden kullanılır.
    /// </summary>
    Task<CategoryChannelMapping?> GetByExternalIdAsync(
        Marketplace marketplace,
        string externalId,
        CancellationToken cancellationToken = default);

    Task AddAsync(CategoryChannelMapping mapping, CancellationToken cancellationToken = default);

    void Update(CategoryChannelMapping mapping);

    void Remove(CategoryChannelMapping mapping);
}
