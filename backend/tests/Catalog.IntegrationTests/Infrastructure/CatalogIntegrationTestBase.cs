namespace Catalog.IntegrationTests.Infrastructure;

/// <summary>Base class for catalog API E2E tests using the shared Postgres fixture.</summary>
[Collection(CatalogIntegrationCollection.Name)]
public abstract class CatalogIntegrationTestBase(CatalogPostgresFixture fixture)
{
    protected HttpClient Client { get; } = CreateClient(fixture);

    private static HttpClient CreateClient(CatalogPostgresFixture fixture)
    {
        CatalogPostgresFixture.SkipIfUnavailable(fixture);
        return IntegrationTestAuth.CreateAuthenticatedClient(fixture.Factory);
    }
}
