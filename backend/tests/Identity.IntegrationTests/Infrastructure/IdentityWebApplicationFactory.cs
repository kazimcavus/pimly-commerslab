using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Identity.IntegrationTests.Infrastructure;

/// <summary>Test host configured against a throwaway identity database.</summary>
public sealed class IdentityWebApplicationFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Not: Minimal hosting'de (WebApplication.CreateBuilder) ConfigureAppConfiguration ile
        // eklenen kaynaklar Program.cs gövdesi builder.Configuration'ı okurken henüz uygulanmaz;
        // bağlantı dizesi gibi servis kaydı sırasında okunan değerler UseSetting ile verilmelidir.
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Database"] = connectionString,
            ["ConnectionStrings:Identity"] = connectionString,
            ["Catalog:AutoMigrate"] = "true",
            ["Identity:AutoMigrate"] = "true",
            ["Identity:Jwt:Secret"] = "integration-test-secret-min-32-bytes-long",
            ["Identity:Jwt:ExpirationHours"] = "1",
            ["Observability:Enabled"] = "false",
        };

        foreach (var (key, value) in settings)
        {
            builder.UseSetting(key, value);
        }
    }
}
