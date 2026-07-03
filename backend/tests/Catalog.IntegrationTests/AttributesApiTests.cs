using System.Net;
using System.Net.Http.Json;
using Catalog.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace Catalog.IntegrationTests;

/// <summary>Attributes API uç noktaları için entegrasyon testleri.</summary>
public class AttributesApiTests(CatalogPostgresFixture fixture) : CatalogIntegrationTestBase(fixture)
{
    [SkippableFact]
    public async Task AttributeCrud_HappyPath()
    {
        var createResponse = await Client.PostAsJsonAsync("/api/v1/catalog/attributes", new
        {
            name = $"Yaka Tipi {Guid.NewGuid():N}",
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<AttributeResponse>();
        created.Should().NotBeNull();
        created!.Name.Should().StartWith("Yaka Tipi");
        created.Key.Should().StartWith("yaka_tipi");

        var addValueResponse = await Client.PostAsJsonAsync($"/api/v1/catalog/attributes/{created.Id}/values", new
        {
            name = "V Yaka",
        });
        addValueResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var value = await addValueResponse.Content.ReadFromJsonAsync<AttributeValueResponse>();
        value.Should().NotBeNull();
        value!.Name.Should().Be("V Yaka");

        var listValuesResponse = await Client.GetAsync($"/api/v1/catalog/attributes/{created.Id}/values");
        listValuesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await Client.GetAsync($"/api/v1/catalog/attributes/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var patchResponse = await Client.PatchAsJsonAsync($"/api/v1/catalog/attributes/{created.Id}", new
        {
            name = "Yaka Tipi Güncel",
        });
        patchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var patchValueResponse = await Client.PatchAsJsonAsync($"/api/v1/catalog/attribute-values/{value!.Id}", new
        {
            name = "Bisiklet Yaka",
        });
        patchValueResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listResponse = await Client.GetAsync("/api/v1/catalog/attributes");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteValueResponse = await Client.DeleteAsync($"/api/v1/catalog/attribute-values/{value.Id}");
        deleteValueResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var deleteResponse = await Client.DeleteAsync($"/api/v1/catalog/attributes/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}

/// <summary>API attribute yanıtını deserialize etmek için kullanılan DTO.</summary>
internal sealed record AttributeResponse(
    Guid Id,
    string Key,
    string Name);

/// <summary>API attribute value yanıtını deserialize etmek için kullanılan DTO.</summary>
internal sealed record AttributeValueResponse(
    Guid Id,
    Guid AttributeId,
    string Name);
