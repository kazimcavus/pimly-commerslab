using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Inventory.Infrastructure.Tenancy;

/// <summary>
/// EF model cache anahtarına tenant kimliğini ekler. Tenant query filter'ı model kurulumunda
/// yakalanan değerle sabitlendiği için, bu factory olmadan ilk model kuran tenant'ın filtresi
/// tüm sürece sabitlenir (veri izolasyon hatası).
/// </summary>
public sealed class InventoryTenantModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) =>
        context is InventoryDbContext inventoryContext
            ? (context.GetType(), inventoryContext.ModelCacheTenantId, designTime)
            : (object)(context.GetType(), designTime);
}
