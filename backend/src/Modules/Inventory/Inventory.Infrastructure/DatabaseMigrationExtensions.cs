using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure;

/// <summary>Inventory veritabanı migration yardımcıları.</summary>
public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Bekleyen EF Core migration'larını uygular.
    /// <c>Inventory:AutoMigrate</c> false ise atlanır (varsayılan: true).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task ApplyInventoryMigrationsAsync(
        this IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue("Inventory:AutoMigrate", defaultValue: true))
        {
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Inventory.DatabaseMigration");
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        logger.LogInformation("Applying inventory database migrations...");
        await db.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Inventory database migrations applied.");
    }
}
