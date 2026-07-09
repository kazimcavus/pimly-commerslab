using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Pricing.Infrastructure.Persistence;

namespace Pricing.Infrastructure.Tenancy;

/// <summary>
/// EF model cache anahtarına tenant kimliğini ekler. Tenant query filter'ı model kurulumunda
/// yakalanan değerle sabitlendiği için, bu factory olmadan ilk model kuran tenant'ın filtresi
/// tüm sürece sabitlenir (veri izolasyon hatası).
/// </summary>
public sealed class PricingTenantModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) =>
        context is PricingDbContext pricingContext
            ? (context.GetType(), pricingContext.ModelCacheTenantId, designTime)
            : (object)(context.GetType(), designTime);
}
