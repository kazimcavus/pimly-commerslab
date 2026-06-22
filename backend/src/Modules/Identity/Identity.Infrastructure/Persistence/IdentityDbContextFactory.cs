using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Identity.Infrastructure.Persistence;

/// <summary>Tasarım zamanı EF Core migration'ları için DbContext fabrikası.</summary>
public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    /// <inheritdoc/>
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("PIMLY_DATABASE_URL")
            ?? "Host=localhost;Port=5432;Database=pimly;Username=pimly;Password=pimly";

        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsHistoryTable("__ef_migrations_history", "identity"));

        return new IdentityDbContext(optionsBuilder.Options);
    }
}
