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
        var categories = await tenantBList.Content.ReadFromJsonAsync<PagedCategoryResponse>();
        categories!.Items.Should().NotContain(c => c.Id == category!.Id);

        var tenantBGet = await tenantBClient.GetAsync($"/api/v1/catalog/categories/{category!.Id}");
        tenantBGet.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Regresyon: EF model cache'i tenant'a göre anahtarlanmazsa ilk sorgulayan tenant'ın
    // query filter'ı sürece sabitlenir ve sonraki tenant kendi verisini bile okuyamaz.
    [SkippableFact]
    public async Task TenantB_CanReadBackItsOwnData_AfterTenantAQueriedFirst()
    {
        var tenantAClient = IntegrationTestAuth.CreateAuthenticatedClient(
            Factory,
            $"model-cache-a-{Guid.NewGuid():N}@example.com",
            "tenant-a-password",
            "Model Cache A",
            "Model Cache Tenant A");

        // Tenant A önce sorgular → hatalı durumda model/filter tenant A'ya sabitlenir.
        var warmup = await tenantAClient.GetAsync("/api/v1/catalog/categories");
        warmup.StatusCode.Should().Be(HttpStatusCode.OK);

        var tenantBClient = IntegrationTestAuth.CreateAuthenticatedClient(
            Factory,
            $"model-cache-b-{Guid.NewGuid():N}@example.com",
            "tenant-b-password",
            "Model Cache B",
            "Model Cache Tenant B");

        var createResponse = await tenantBClient.PostAsJsonAsync("/api/v1/catalog/categories", new
        {
            name = $"OwnData-{Guid.NewGuid():N}",
            parent_id = (Guid?)null,
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var category = await createResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        // Tenant B kendi oluşturduğu kategoriyi hem listede hem tekil GET'te görmeli.
        var tenantBList = await tenantBClient.GetAsync("/api/v1/catalog/categories");
        tenantBList.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await tenantBList.Content.ReadFromJsonAsync<PagedCategoryResponse>();
        categories!.Items.Should().Contain(c => c.Id == category!.Id);

        var tenantBGet = await tenantBClient.GetAsync($"/api/v1/catalog/categories/{category!.Id}");
        tenantBGet.StatusCode.Should().Be(HttpStatusCode.OK);

        // Tenant A hâlâ tenant B'nin verisini görmemeli.
        var tenantAGet = await tenantAClient.GetAsync($"/api/v1/catalog/categories/{category!.Id}");
        tenantAGet.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record PagedCategoryResponse(List<CategoryResponse> Items, int Page, int PageSize, int TotalCount);
}
