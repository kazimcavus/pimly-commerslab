using Channels.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Channels.Infrastructure;

/// <summary>Channels veritabanı migration yardımcıları.</summary>
public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Bekleyen EF Core migration'larını uygular.
    /// <c>Channels:AutoMigrate</c> false ise atlanır (varsayılan: true).
    /// </summary>
    public static async Task ApplyChannelsMigrationsAsync(
        this IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue("Channels:AutoMigrate", defaultValue: true))
        {
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Channels.DatabaseMigration");
        var db = scope.ServiceProvider.GetRequiredService<ChannelsDbContext>();

        logger.LogInformation("Applying channels database migrations...");
        await db.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Channels database migrations applied.");
    }
}
