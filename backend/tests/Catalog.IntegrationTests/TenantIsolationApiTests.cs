using System.Net;
using System.Net.Http.Json;
using Catalog.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace Catalog.IntegrationTests;

/// <summary>Tenant izolasyonu için entegrasyon testleri.</summary>
public class TenantIsolationApiTests(CatalogPostgresFixture fixture) : CatalogIntegrationTestBase(fixture)
{
    [SkippableFact]
    public async Task CategoryCreatedByTenantA_IsNotVisibleToTenantB()
    {
        var tenantAClient = IntegrationTestAuth.CreateAuthenticatedClient(
            Factory,
            "tenant-a@example.com",
            "tenant-a-password",
            "Tenant A User",
            "Tenant A");

        var tenantBClient = IntegrationTestAuth.CreateAuthenticatedClient(
            Factory,
            "tenant-b@example.com",
            "tenant-b-password",
            "Tenant B User",
            "Tenant B");

        var createResponse = await tenantAClient.PostAsJsonAsync("/api/v1/catalog/categories", new
        {
            name = $"Isolated-{Guid.NewGuid():N}",
            parent_id = (Guid?)null,
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var category = await createResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        var tenantBList = await tenantBClient.GetAsync("/api/v1/catalog/categories");
        tenantBList.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await tenantBList.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        categories.Should().NotContain(c => c.Id == category!.Id);

        var tenantBGet = await tenantBClient.GetAsync($"/api/v1/catalog/categories/{category!.Id}");
        tenantBGet.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
