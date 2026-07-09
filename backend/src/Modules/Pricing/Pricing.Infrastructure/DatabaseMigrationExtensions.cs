using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pricing.Infrastructure.Persistence;

namespace Pricing.Infrastructure;

/// <summary>Pricing veritabanı migration yardımcıları.</summary>
public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Bekleyen EF Core migration'larını uygular.
    /// <c>Pricing:AutoMigrate</c> false ise atlanır (varsayılan: true).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task ApplyPricingMigrationsAsync(
        this IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue("Pricing:AutoMigrate", defaultValue: true))
        {
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Pricing.DatabaseMigration");
        var db = scope.ServiceProvider.GetRequiredService<PricingDbContext>();

        logger.LogInformation("Applying pricing database migrations...");
        await db.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Pricing database migrations applied.");
    }
}
