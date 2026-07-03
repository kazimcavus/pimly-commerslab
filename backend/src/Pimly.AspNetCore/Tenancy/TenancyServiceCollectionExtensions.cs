using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Tenancy;

namespace Pimly.AspNetCore.Tenancy;

/// <summary>Multi-tenant HTTP bağlamı DI kayıtları.</summary>
public static class TenancyServiceCollectionExtensions
{
    /// <summary>JWT tabanlı tenant bağlamını servis koleksiyonuna ekler.</summary>
    public static IServiceCollection AddPimlyTenancy(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, HttpTenantContext>();
        return services;
    }
}
