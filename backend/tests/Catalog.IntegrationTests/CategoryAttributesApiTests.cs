using System.Net;
using System.Net.Http.Json;
using Catalog.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace Catalog.IntegrationTests;

/// <summary>Kategori-öznitelik ataması API uç noktaları için entegrasyon testleri.</summary>
public class CategoryAttributesApiTests(CatalogPostgresFixture fixture) : CatalogIntegrationTestBase(fixture)
{
    [SkippableFact]
    public async Task CategoryAttributeAssignment_HappyPath()
    {
        var categoryResponse = await Client.PostAsJsonAsync("/api/v1/catalog/categories", new
        {
            name = $"Attr Category {Guid.NewGuid():N}",
            parent_id = (Guid?)null,
        });
        categoryResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        category.Should().NotBeNull();

        var attributeResponse = await Client.PostAsJsonAsync("/api/v1/catalog/attributes", new
        {
            name = $"Material {Guid.NewGuid():N}",
        });
        attributeResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var attribute = await attributeResponse.Content.ReadFromJsonAsync<AttributeSummaryResponse>();
        attribute.Should().NotBeNull();

        var assignResponse = await Client.PostAsJsonAsync(
            $"/api/v1/catalog/categories/{category!.Id}/attributes",
            new { attributeId = attribute!.Id, required = true, sortOrder = 0 });
        assignResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var assignment = await assignResponse.Content.ReadFromJsonAsync<CategoryAttributeResponse>();
        assignment.Should().NotBeNull();
        assignment!.AttributeId.Should().Be(attribute.Id);
        assignment.Required.Should().BeTrue();

        var listResponse = await Client.GetAsync($"/api/v1/catalog/categories/{category.Id}/attributes");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var patchResponse = await Client.PatchAsJsonAsync(
            $"/api/v1/catalog/category-attributes/{assignment.CategoryAttributeId}",
            new { required = false, sortOrder = 1 });
        patchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await patchResponse.Content.ReadFromJsonAsync<CategoryAttributeResponse>();
        updated!.Required.Should().BeFalse();
        updated.SortOrder.Should().Be(1);

        var removeResponse = await Client.DeleteAsync(
            $"/api/v1/catalog/category-attributes/{assignment.CategoryAttributeId}");
        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await Client.DeleteAsync($"/api/v1/catalog/attributes/{attribute.Id}");
        await Client.DeleteAsync($"/api/v1/catalog/categories/{category.Id}");
    }
}

/// <summary>Kategori-attribute ilişkisinde özet attribute API yanıtını deserialize etmek için kullanılan DTO.</summary>
internal sealed record AttributeSummaryResponse(Guid Id);

/// <summary>Kategoriye atanmış attribute API yanıtını deserialize etmek için kullanılan DTO.</summary>
internal sealed record CategoryAttributeResponse(
    Guid CategoryAttributeId,
    Guid AttributeId,
    bool Required,
    int SortOrder);
