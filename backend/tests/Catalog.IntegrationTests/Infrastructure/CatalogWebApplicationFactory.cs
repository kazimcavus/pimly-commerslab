using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Catalog.IntegrationTests.Infrastructure;

/// <summary>Test host configured against a throwaway catalog database.</summary>
public sealed class CatalogWebApplicationFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = connectionString,
                ["ConnectionStrings:Identity"] = connectionString,
                ["Catalog:AutoMigrate"] = "true",
                ["Identity:AutoMigrate"] = "true",
                ["Identity:Jwt:Secret"] = "integration-test-secret",
                ["Identity:Jwt:ExpirationHours"] = "1",
            });
        });
    }
}
