using System.Net;
using System.Net.Http.Json;
using Catalog.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace Catalog.IntegrationTests;

/// <summary>Variants ve values API uç noktaları için entegrasyon testleri.</summary>
public class VariantsApiTests(CatalogPostgresFixture fixture) : CatalogIntegrationTestBase(fixture)
{
    [SkippableFact]
    public async Task VariantAndValuesCrud_HappyPath()
    {
        var createVariantResponse = await Client.PostAsJsonAsync("/api/v1/catalog/variants", new
        {
            name = $"Color-{Guid.NewGuid():N}",
            selectionStyle = "color",
            sortOrder = 0,
            slicer = false,
        });
        createVariantResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var variant = await createVariantResponse.Content.ReadFromJsonAsync<VariantResponse>();
        variant.Should().NotBeNull();

        var createValueResponse = await Client.PostAsJsonAsync(
            $"/api/v1/catalog/variants/{variant!.Id}/values",
            new { label = "Red", color = "#ff0000", sortOrder = 0 });
        createValueResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var variantValue = await createValueResponse.Content.ReadFromJsonAsync<VariantValueResponse>();
        variantValue.Should().NotBeNull();
        variantValue!.Label.Should().Be("Red");

        var listValuesResponse = await Client.GetAsync($"/api/v1/catalog/variants/{variant.Id}/values");
        listValuesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var patchVariantResponse = await Client.PatchAsJsonAsync($"/api/v1/catalog/variants/{variant.Id}", new
        {
            name = variant.Name,
            selectionStyle = "color",
            sortOrder = 1,
            slicer = true,
        });
        patchVariantResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var patchValueResponse = await Client.PatchAsJsonAsync($"/api/v1/catalog/variant-values/{variantValue!.Id}", new
        {
            label = "Crimson",
            color = "#dc143c",
            sortOrder = 1,
        });
        patchValueResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listVariantsResponse = await Client.GetAsync("/api/v1/catalog/variants");
        listVariantsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteValueResponse = await Client.DeleteAsync($"/api/v1/catalog/variant-values/{variantValue.Id}");
        deleteValueResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var deleteVariantResponse = await Client.DeleteAsync($"/api/v1/catalog/variants/{variant.Id}");
        deleteVariantResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [SkippableFact]
    public async Task GetVariantType_ById_ReturnsCreated()
    {
        var createVariantResponse = await Client.PostAsJsonAsync("/api/v1/catalog/variants", new
        {
            name = $"Lookup-{Guid.NewGuid():N}",
            selectionStyle = "list",
            sortOrder = 0,
            slicer = false,
        });
        createVariantResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createVariantResponse.Content.ReadFromJsonAsync<VariantResponse>();

        var getResponse = await Client.GetAsync($"/api/v1/catalog/variants/{created!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<VariantResponse>();
        fetched!.Id.Should().Be(created.Id);
        fetched.Name.Should().Be(created.Name);

        await Client.DeleteAsync($"/api/v1/catalog/variants/{created.Id}");
    }
}

internal sealed record VariantResponse(Guid Id, string Name, bool Slicer);

internal sealed record VariantValueResponse(Guid Id, string Label, string? Color);
