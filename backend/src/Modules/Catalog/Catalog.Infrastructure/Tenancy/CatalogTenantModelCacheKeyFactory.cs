using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Catalog.Infrastructure.Tenancy;

/// <summary>
/// EF model cache anahtarına tenant kimliğini ekler. Tenant query filter'ı model kurulumunda
/// yakalanan değerle sabitlendiği için, bu factory olmadan ilk model kuran tenant'ın filtresi
/// tüm sürece sabitlenir (veri izolasyon hatası).
/// </summary>
public sealed class CatalogTenantModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) =>
        context is CatalogDbContext catalogContext
            ? (context.GetType(), catalogContext.ModelCacheTenantId, designTime)
            : (object)(context.GetType(), designTime);
}
