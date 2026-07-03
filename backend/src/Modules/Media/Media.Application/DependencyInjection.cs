using FluentValidation;
using Media.Application.UploadImage;
using Microsoft.Extensions.DependencyInjection;

namespace Media.Application;

/// <summary>Media.Application modülü için bağımlılık enjeksiyonu yapılandırması.</summary>
public static class DependencyInjection
{
    /// <summary>Media application servislerini kaydeder.</summary>
    public static IServiceCollection AddMediaApplication(this IServiceCollection services)
    {
        services.AddScoped<IUploadImageHandler, UploadImageHandler>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
