using Channels.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Channels.Infrastructure.Tenancy;

/// <summary>
/// EF model cache anahtarına tenant kimliğini ekler. Tenant query filter'ı model kurulumunda
/// yakalanan değerle sabitlendiği için, bu factory olmadan ilk model kuran tenant'ın filtresi
/// tüm sürece sabitlenir (veri izolasyon hatası).
/// </summary>
public sealed class ChannelsTenantModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) =>
        context is ChannelsDbContext channelsContext
            ? (context.GetType(), channelsContext.ModelCacheTenantId, designTime)
            : (object)(context.GetType(), designTime);
}
