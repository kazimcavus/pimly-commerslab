using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Pimly.AspNetCore.Observability;

/// <summary>Media blob depolama dizininin erişilebilirliğini doğrular.</summary>
internal sealed class MediaStorageHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var storagePath = configuration["Media:StoragePath"] ?? "./storage/media";
        var fullPath = Path.GetFullPath(storagePath);

        if (!Directory.Exists(fullPath))
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Media storage directory does not exist: {fullPath}"));
        }

        try
        {
            var probeFile = Path.Combine(fullPath, $".health-{Guid.NewGuid():N}");
            File.WriteAllText(probeFile, "ok");
            File.Delete(probeFile);
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Media storage directory is not writable: {fullPath}",
                ex));
        }

        return Task.FromResult(HealthCheckResult.Healthy($"Media storage is writable: {fullPath}"));
    }
}
