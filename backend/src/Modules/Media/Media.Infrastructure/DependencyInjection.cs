using Media.Application.Options;
using Media.Application.Storage;
using Media.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Media.Infrastructure;

/// <summary>Media altyapı servislerini DI konteynerine kaydeder.</summary>
public static class DependencyInjection
{
    /// <summary>Media infrastructure servislerini kaydeder.</summary>
    public static IServiceCollection AddMediaInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MediaOptions>(configuration.GetSection(MediaOptions.SectionName));
        services.AddSingleton<IBlobStorage, LocalBlobStorage>();
        services.AddSingleton<IImageContentTypeDetector, ImageContentTypeDetectorAdapter>();

        return services;
    }
}
