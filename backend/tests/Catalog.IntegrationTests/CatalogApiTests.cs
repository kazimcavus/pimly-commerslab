using System.Net;
using System.Net.Http.Json;
using Catalog.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace Catalog.IntegrationTests;

/// <summary>Catalog API uç noktaları için entegrasyon testleri.</summary>
public class CatalogApiTests(CatalogPostgresFixture fixture) : CatalogIntegrationTestBase(fixture)
{
    [SkippableFact]
    public async Task Healthz_ReturnsOk()
    {
        var response = await Client.GetAsync("/healthz");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task CategoryCrud_HappyPath()
    {
        var createResponse = await Client.PostAsJsonAsync("/api/v1/catalog/categories", new
        {
            name = "Integration Category",
            code = "INT",
            parent_id = (Guid?)null,
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        created.Should().NotBeNull();
        created!.Name.Should().Be("Integration Category");

        var listResponse = await Client.GetAsync("/api/v1/catalog/categories");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteResponse = await Client.DeleteAsync($"/api/v1/catalog/categories/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [SkippableFact]
    public async Task CategoryHierarchy_ParentChild_CreateAndGet()
    {
        var parentResponse = await Client.PostAsJsonAsync("/api/v1/catalog/categories", new
        {
            name = $"Parent {Guid.NewGuid():N}",
            parent_id = (Guid?)null,
        });
        parentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var parent = await parentResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        var childResponse = await Client.PostAsJsonAsync("/api/v1/catalog/categories", new
        {
            name = $"Child {Guid.NewGuid():N}",
            parent_id = parent!.Id,
        });
        childResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var child = await childResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        var getResponse = await Client.GetAsync($"/api/v1/catalog/categories/{child!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        fetched!.ParentId.Should().Be(parent.Id);

        await Client.DeleteAsync($"/api/v1/catalog/categories/{child.Id}");
        await Client.DeleteAsync($"/api/v1/catalog/categories/{parent.Id}");
    }

    [SkippableFact]
    public async Task CategoryPatch_UpdatesNameAndCode()
    {
        var createResponse = await Client.PostAsJsonAsync("/api/v1/catalog/categories", new
        {
            name = "Patch Me",
            code = "OLD",
            parent_id = (Guid?)null,
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        var patchResponse = await Client.PatchAsJsonAsync($"/api/v1/catalog/categories/{created!.Id}", new
        {
            name = "Patched",
            code = "NEW",
            parent_id = (Guid?)null,
        });
        patchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await Client.GetAsync($"/api/v1/catalog/categories/{created.Id}");
        var updated = await getResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        updated!.Name.Should().Be("Patched");
        updated.Code.Should().Be("NEW");

        await Client.DeleteAsync($"/api/v1/catalog/categories/{created.Id}");
    }
}

/// <summary>API kategori yanıtını deserialize etmek için kullanılan DTO.</summary>
internal sealed record CategoryResponse(Guid Id, string Name, string? Code, Guid? ParentId);
