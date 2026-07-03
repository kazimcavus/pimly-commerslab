using Channels.Application.ExternalCatalog;
using Channels.Domain.Marketplaces;
using SharedKernel;

namespace Channels.Infrastructure.Taxonomy;

/// <summary>Geliştirme ve test için örnek Trendyol kategori ağacı döndürür.</summary>
internal sealed class StubMarketplaceTaxonomyClient : IMarketplaceTaxonomyClient
{
    /// <inheritdoc/>
    public Task<Result<IReadOnlyList<MarketplaceCategoryNode>>> FetchAllCategoriesAsync(
        Marketplace marketplace,
        CancellationToken cancellationToken = default)
    {
        _ = marketplace;
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<MarketplaceCategoryNode> categories =
        [
            new("100", "Elektronik", null, "Elektronik", false),
            new("110", "Telefon", "100", "Elektronik > Telefon", false),
            new("111", "Akıllı Telefon", "110", "Elektronik > Telefon > Akıllı Telefon", true),
            new("112", "Telefon Aksesuarları", "110", "Elektronik > Telefon > Telefon Aksesuarları", true),
            new("120", "Bilgisayar", "100", "Elektronik > Bilgisayar", false),
            new("121", "Dizüstü Bilgisayar", "120", "Elektronik > Bilgisayar > Dizüstü Bilgisayar", true),
            new("200", "Moda", null, "Moda", false),
            new("210", "Kadın", "200", "Moda > Kadın", false),
            new("211", "Elbise", "210", "Moda > Kadın > Elbise", true),
            new("212", "Ayakkabı", "210", "Moda > Kadın > Ayakkabı", true),
            new("220", "Erkek", "200", "Moda > Erkek", false),
            new("221", "Gömlek", "220", "Moda > Erkek > Gömlek", true),
        ];

        return Task.FromResult(Result.Success(categories));
    }
}
