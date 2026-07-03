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
            selection_style = "color",
            sort_order = 0,
            slicer = false,
        });
        createVariantResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var variant = await createVariantResponse.Content.ReadFromJsonAsync<VariantResponse>(CatalogJson.Options);
        variant.Should().NotBeNull();

        var createValueResponse = await Client.PostAsJsonAsync(
            $"/api/v1/catalog/variants/{variant!.Id}/values",
            new { label = "Red", color = "#ff0000", sort_order = 0 });
        createValueResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var variantValue = await createValueResponse.Content.ReadFromJsonAsync<VariantValueResponse>(CatalogJson.Options);
        variantValue.Should().NotBeNull();
        variantValue!.Label.Should().Be("Red");

        var listValuesResponse = await Client.GetAsync($"/api/v1/catalog/variants/{variant.Id}/values");
        listValuesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var patchVariantResponse = await Client.PatchAsJsonAsync($"/api/v1/catalog/variants/{variant.Id}", new
        {
            name = variant.Name,
            selection_style = "color",
            sort_order = 1,
            slicer = true,
        });
        patchVariantResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var patchValueResponse = await Client.PatchAsJsonAsync($"/api/v1/catalog/variant-values/{variantValue!.Id}", new
        {
            label = "Crimson",
            color = "#dc143c",
            sort_order = 1,
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
            selection_style = "list",
            sort_order = 0,
            slicer = false,
        });
        createVariantResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createVariantResponse.Content.ReadFromJsonAsync<VariantResponse>(CatalogJson.Options);

        var getResponse = await Client.GetAsync($"/api/v1/catalog/variants/{created!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<VariantResponse>(CatalogJson.Options);
        fetched!.Id.Should().Be(created.Id);
        fetched.Name.Should().Be(created.Name);

        await Client.DeleteAsync($"/api/v1/catalog/variants/{created.Id}");
    }

    [SkippableFact]
    public async Task CreateVariantType_SecondSlicer_Returns409()
    {
        var firstResponse = await Client.PostAsJsonAsync("/api/v1/catalog/variants", new
        {
            name = $"Slicer-A-{Guid.NewGuid():N}",
            selection_style = "color",
            sort_order = 0,
            slicer = true,
        });
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var first = await firstResponse.Content.ReadFromJsonAsync<VariantResponse>(CatalogJson.Options);

        var secondResponse = await Client.PostAsJsonAsync("/api/v1/catalog/variants", new
        {
            name = $"Slicer-B-{Guid.NewGuid():N}",
            selection_style = "list",
            sort_order = 1,
            slicer = true,
        });
        await CatalogHttpAssertions.AssertProblemAsync(secondResponse, HttpStatusCode.Conflict, "conflict");

        await Client.DeleteAsync($"/api/v1/catalog/variants/{first!.Id}");
    }

    [SkippableFact]
    public async Task UpdateVariantType_SecondSlicer_Returns409()
    {
        var slicerResponse = await Client.PostAsJsonAsync("/api/v1/catalog/variants", new
        {
            name = $"Slicer-A-{Guid.NewGuid():N}",
            selection_style = "color",
            sort_order = 0,
            slicer = true,
        });
        slicerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var slicer = await slicerResponse.Content.ReadFromJsonAsync<VariantResponse>(CatalogJson.Options);

        var otherResponse = await Client.PostAsJsonAsync("/api/v1/catalog/variants", new
        {
            name = $"Other-{Guid.NewGuid():N}",
            selection_style = "list",
            sort_order = 1,
            slicer = false,
        });
        otherResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var other = await otherResponse.Content.ReadFromJsonAsync<VariantResponse>(CatalogJson.Options);

        var patchResponse = await Client.PatchAsJsonAsync($"/api/v1/catalog/variants/{other!.Id}", new
        {
            name = other.Name,
            selection_style = "list",
            sort_order = 1,
            slicer = true,
        });
        await CatalogHttpAssertions.AssertProblemAsync(patchResponse, HttpStatusCode.Conflict, "conflict");

        await Client.DeleteAsync($"/api/v1/catalog/variants/{other.Id}");
        await Client.DeleteAsync($"/api/v1/catalog/variants/{slicer!.Id}");
    }
}

/// <summary>Variant type API yanıtını deserialize etmek için kullanılan DTO.</summary>
internal sealed record VariantResponse(Guid Id, string Name, bool Slicer);

/// <summary>Variant value API yanıtını deserialize etmek için kullanılan DTO.</summary>
internal sealed record VariantValueResponse(Guid Id, string Label, string? Color);
