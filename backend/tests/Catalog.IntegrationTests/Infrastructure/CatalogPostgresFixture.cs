using Testcontainers.PostgreSql;

namespace Catalog.IntegrationTests.Infrastructure;

/// <summary>PostgreSQL Testcontainer for isolated API E2E tests.</summary>
public sealed class CatalogPostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("pimly")
        .WithUsername("pimly")
        .WithPassword("pimly")
        .Build();

    public bool IsAvailable { get; private set; }

    public CatalogWebApplicationFactory Factory { get; private set; } = null!;

    public static void SkipIfUnavailable(CatalogPostgresFixture fixture)
    {
        Skip.If(!fixture.IsAvailable, "Docker is not available. Start Docker to run integration tests with Testcontainers.");
    }

    public async Task InitializeAsync()
    {
        try
        {
            await _postgres.StartAsync();
            Factory = new CatalogWebApplicationFactory(_postgres.GetConnectionString());
            _ = Factory.CreateClient();
            IsAvailable = true;
        }
        catch (Exception)
        {
            IsAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (IsAvailable)
        {
            Factory.Dispose();
        }

        await _postgres.DisposeAsync().AsTask();
    }
}

/// <summary>Shared PostgreSQL fixture for all catalog integration tests.</summary>
[CollectionDefinition(Name)]
public sealed class CatalogIntegrationCollection : ICollectionFixture<CatalogPostgresFixture>
{
    public const string Name = "CatalogIntegration";
}
