using Testcontainers.PostgreSql;

namespace Identity.IntegrationTests.Infrastructure;

/// <summary>PostgreSQL Testcontainer for isolated identity API E2E tests.</summary>
public sealed class IdentityPostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;

    public bool IsAvailable { get; private set; }

    public IdentityWebApplicationFactory Factory { get; private set; } = null!;

    public static void SkipIfUnavailable(IdentityPostgresFixture fixture)
    {
        Skip.If(!fixture.IsAvailable, "Docker is not available. Start Docker to run integration tests with Testcontainers.");
    }

    public async Task InitializeAsync()
    {
        try
        {
            _postgres = new PostgreSqlBuilder("postgres:17-alpine")
                .WithDatabase("pimly")
                .WithUsername("pimly")
                .WithPassword("pimly")
                .Build();

            await _postgres.StartAsync();
            Factory = new IdentityWebApplicationFactory(_postgres.GetConnectionString());
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

        if (_postgres is not null)
        {
            await _postgres.DisposeAsync().AsTask();
        }
    }
}

/// <summary>Shared PostgreSQL fixture for all identity integration tests.</summary>
[CollectionDefinition(Name)]
public sealed class IdentityIntegrationCollection : ICollectionFixture<IdentityPostgresFixture>
{
    public const string Name = "IdentityIntegration";
}
