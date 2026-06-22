using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure;

/// <summary>Catalog veritabanı migration yardımcıları.</summary>
public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Bekleyen EF Core migration'larını uygular.
    /// <c>Catalog:AutoMigrate</c> false ise atlanır (varsayılan: true).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task ApplyCatalogMigrationsAsync(
        this IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue("Catalog:AutoMigrate", defaultValue: true))
        {
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Catalog.DatabaseMigration");
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        logger.LogInformation("Applying catalog database migrations...");
        await db.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Catalog database migrations applied.");
    }
}
