using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Catalog.IntegrationTests.Infrastructure;

/// <summary>Entegrasyon testlerinde katalog varlıkları oluşturmak için yardımcılar.</summary>
internal static class CatalogTestData
{
    internal static async Task<Guid> CreateCategoryAsync(HttpClient client, string? name = null)
    {
        var response = await client.PostAsJsonAsync("/api/v1/catalog/categories", new
        {
            name = name ?? $"Cat-{Guid.NewGuid():N}",
            parent_id = (Guid?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var category = await response.Content.ReadFromJsonAsync<CategoryResponse>(CatalogJson.Options);
        return category!.Id;
    }
}

/// <summary>Kategori API yanıtını deserialize etmek için kullanılan DTO.</summary>
internal sealed record CategoryResponse(Guid Id, string Name, string? Code, Guid? ParentId);
