using Inventory.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Inventory.Infrastructure.Persistence;

/// <summary>Tasarım zamanı EF Core migration'ları için DbContext fabrikası.</summary>
public sealed class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    /// <inheritdoc/>
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("PIMLY_DATABASE_URL")
            ?? "Host=localhost;Port=5432;Database=pimly;Username=pimly;Password=pimly";

        var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsHistoryTable("__ef_migrations_history", "inventory"));

        return new InventoryDbContext(optionsBuilder.Options, new DesignTimeTenantContext());
    }
}
