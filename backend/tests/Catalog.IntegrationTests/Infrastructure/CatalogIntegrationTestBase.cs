namespace Catalog.IntegrationTests.Infrastructure;

/// <summary>Base class for catalog API E2E tests using the shared Postgres fixture.</summary>
[Collection(CatalogIntegrationCollection.Name)]
public abstract class CatalogIntegrationTestBase(CatalogPostgresFixture fixture)
{
    protected HttpClient Client { get; } = CreateClient(fixture);

    protected CatalogWebApplicationFactory Factory { get; } = fixture.Factory;

    protected Task<Guid> CreateCategoryAsync(string? name = null) =>
        CatalogTestData.CreateCategoryAsync(Client, name);

    private static HttpClient CreateClient(CatalogPostgresFixture fixture)
    {
        CatalogPostgresFixture.SkipIfUnavailable(fixture);

        // Testler tek bir Postgres veritabanını paylaşır; slicer/attribute anahtarı gibi
        // tenant başına tekil kayıtlarda çapraz test çakışmasını önlemek için her test
        // örneği kendi tenant'ı ile çalışır.
        var unique = Guid.NewGuid().ToString("N");
        return IntegrationTestAuth.CreateAuthenticatedClient(
            fixture.Factory,
            $"catalog-tests-{unique}@example.com",
            "integration-test-password",
            "Catalog Test User",
            $"Catalog Test Tenant {unique}");
    }
}
