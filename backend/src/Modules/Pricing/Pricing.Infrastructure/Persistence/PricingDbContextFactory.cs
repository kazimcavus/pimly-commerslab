using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pricing.Infrastructure.Tenancy;

namespace Pricing.Infrastructure.Persistence;

/// <summary>Tasarım zamanı EF Core migration'ları için DbContext fabrikası.</summary>
public sealed class PricingDbContextFactory : IDesignTimeDbContextFactory<PricingDbContext>
{
    /// <inheritdoc/>
    public PricingDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("PIMLY_DATABASE_URL")
            ?? "Host=localhost;Port=5432;Database=pimly;Username=pimly;Password=pimly";

        var optionsBuilder = new DbContextOptionsBuilder<PricingDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsHistoryTable("__ef_migrations_history", "pricing"));

        return new PricingDbContext(optionsBuilder.Options, new DesignTimeTenantContext());
    }
}
