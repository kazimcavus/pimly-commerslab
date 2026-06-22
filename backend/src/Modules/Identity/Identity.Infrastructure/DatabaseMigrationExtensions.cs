using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure;

/// <summary>Identity veritabanı migration yardımcıları.</summary>
public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Bekleyen EF Core migration'larını uygular.
    /// <c>Identity:AutoMigrate</c> false ise atlanır (varsayılan: true).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task ApplyIdentityMigrationsAsync(
        this IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue("Identity:AutoMigrate", defaultValue: true))
        {
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Identity.DatabaseMigration");
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        logger.LogInformation("Applying identity database migrations...");
        await db.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Identity database migrations applied.");
    }
}
