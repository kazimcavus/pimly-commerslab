using Channels.Infrastructure.Persistence;
using Channels.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Channels.Infrastructure.Persistence;

/// <summary>Tasarım zamanı EF Core migration'ları için DbContext fabrikası.</summary>
public sealed class ChannelsDbContextFactory : IDesignTimeDbContextFactory<ChannelsDbContext>
{
    /// <inheritdoc/>
    public ChannelsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("PIMLY_DATABASE_URL")
            ?? "Host=localhost;Port=5432;Database=pimly;Username=pimly;Password=pimly";

        var optionsBuilder = new DbContextOptionsBuilder<ChannelsDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsHistoryTable("__ef_migrations_history", "channels"));

        return new ChannelsDbContext(optionsBuilder.Options, new DesignTimeTenantContext());
    }
}
