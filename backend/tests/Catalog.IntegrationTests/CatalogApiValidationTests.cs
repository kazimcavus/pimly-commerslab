using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Catalog.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace Catalog.IntegrationTests;

/// <summary>Attributes API hata yolu E2E testleri.</summary>
public class AttributesApiValidationTests(CatalogPostgresFixture fixture) : CatalogIntegrationTestBase(fixture)
{
    [SkippableFact]
    public async Task CreateAttribute_EmptyName_Returns400()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/catalog/attributes", new { name = "  " });
        await CatalogHttpAssertions.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");
    }

    [SkippableFact]
    public async Task GetAttribute_UnknownId_Returns404()
    {
        var response = await Client.GetAsync($"/api/v1/catalog/attributes/{Guid.NewGuid()}");
        await CatalogHttpAssertions.AssertProblemAsync(response, HttpStatusCode.NotFound, "not_found");
    }

    [SkippableFact]
    public async Task DeleteAttribute_UnknownId_Returns404()
    {
        var response = await Client.DeleteAsync($"/api/v1/catalog/attributes/{Guid.NewGuid()}");
        await CatalogHttpAssertions.AssertProblemAsync(response, HttpStatusCode.NotFound, "not_found");
    }

    [SkippableFact]
    public async Task AddAttributeValue_DuplicateName_Returns409()
    {
        var attributeResponse = await Client.PostAsJsonAsync("/api/v1/catalog/attributes", new
        {
            name = $"Material {Guid.NewGuid():N}",
        });
        attributeResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var attribute = await attributeResponse.Content.ReadFromJsonAsync<AttributeResponse>();

        (await Client.PostAsJsonAsync($"/api/v1/catalog/attributes/{attribute!.Id}/values", new { name = "Cotton" }))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicate = await Client.PostAsJsonAsync($"/api/v1/catalog/attributes/{attribute.Id}/values", new { name = "cotton" });
        await CatalogHttpAssertions.AssertProblemAsync(duplicate, HttpStatusCode.Conflict, "conflict");

        await Client.DeleteAsync($"/api/v1/catalog/attributes/{attribute.Id}");
    }
}

/// <summary>Variants API hata yolu E2E testleri.</summary>
public class VariantsApiValidationTests(CatalogPostgresFixture fixture) : CatalogIntegrationTestBase(fixture)
{
    [SkippableFact]
    public async Task CreateVariant_EmptyName_Returns400()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/catalog/variants", new
        {
            name = "  ",
            selectionStyle = "list",
            sortOrder = 0,
            slicer = false,
        });

        await CatalogHttpAssertions.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");
    }

    [SkippableFact]
    public async Task CreateVariant_InvalidSelectionStyle_Returns400()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/catalog/variants", new
        {
            name = $"Invalid-{Guid.NewGuid():N}",
            selectionStyle = "unknown",
            sortOrder = 0,
            slicer = false,
        });

        await CatalogHttpAssertions.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");
    }

    [SkippableFact]
    public async Task GetVariant_UnknownId_Returns404()
    {
        var response = await Client.GetAsync($"/api/v1/catalog/variants/{Guid.NewGuid()}");
        await CatalogHttpAssertions.AssertProblemAsync(response, HttpStatusCode.NotFound, "not_found");
    }

    [SkippableFact]
    public async Task AddVariantValue_DuplicateLabel_Returns409()
    {
        var variantResponse = await Client.PostAsJsonAsync("/api/v1/catalog/variants", new
        {
            name = $"Color-{Guid.NewGuid():N}",
            selectionStyle = "color",
            sortOrder = 0,
            slicer = false,
        });
        variantResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var variant = await variantResponse.Content.ReadFromJsonAsync<VariantResponse>();

        (await Client.PostAsJsonAsync($"/api/v1/catalog/variants/{variant!.Id}/values", new { label = "Red", color = "#ff0000", sortOrder = 0 }))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicate = await Client.PostAsJsonAsync($"/api/v1/catalog/variants/{variant.Id}/values", new { label = "red", color = "#00ff00", sortOrder = 1 });
        await CatalogHttpAssertions.AssertProblemAsync(duplicate, HttpStatusCode.Conflict, "conflict");

        await Client.DeleteAsync($"/api/v1/catalog/variants/{variant.Id}");
    }
}

/// <summary>Categories ve batch API hata yolu E2E testleri.</summary>
public class CatalogApiValidationTests(CatalogPostgresFixture fixture) : CatalogIntegrationTestBase(fixture)
{
    [SkippableFact]
    public async Task CreateCategory_EmptyName_Returns400()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/catalog/categories", new { name = "  ", parent_id = (Guid?)null });
        await CatalogHttpAssertions.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");
    }

    [SkippableFact]
    public async Task GetCategory_UnknownId_Returns404()
    {
        var response = await Client.GetAsync($"/api/v1/catalog/categories/{Guid.NewGuid()}");
        await CatalogHttpAssertions.AssertProblemAsync(response, HttpStatusCode.NotFound, "not_found");
    }

    [SkippableFact]
    public async Task PatchCategory_UnknownId_Returns404()
    {
        var response = await Client.PatchAsJsonAsync($"/api/v1/catalog/categories/{Guid.NewGuid()}", new { name = "Missing", code = "X" });
        await CatalogHttpAssertions.AssertProblemAsync(response, HttpStatusCode.NotFound, "not_found");
    }

    [SkippableFact]
    public async Task DeleteCategory_UnknownId_Returns404()
    {
        var response = await Client.DeleteAsync($"/api/v1/catalog/categories/{Guid.NewGuid()}");
        await CatalogHttpAssertions.AssertProblemAsync(response, HttpStatusCode.NotFound, "not_found");
    }

    [SkippableFact]
    public async Task ListCategoryAttributes_UnknownCategory_Returns404()
    {
        var response = await Client.GetAsync($"/api/v1/catalog/categories/{Guid.NewGuid()}/attributes");
        await CatalogHttpAssertions.AssertProblemAsync(response, HttpStatusCode.NotFound, "not_found");
    }

    [SkippableFact]
    public async Task AssignCategoryAttribute_DuplicateAssign_Returns409()
    {
        var category = await Client.PostAsJsonAsync("/api/v1/catalog/categories", new { name = $"Cat {Guid.NewGuid():N}", parent_id = (Guid?)null });
        var categoryId = (await category.Content.ReadFromJsonAsync<CategoryResponse>())!.Id;

        var attribute = await Client.PostAsJsonAsync("/api/v1/catalog/attributes", new { name = $"Attr {Guid.NewGuid():N}" });
        var attributeId = (await attribute.Content.ReadFromJsonAsync<AttributeSummaryResponse>())!.Id;

        (await Client.PostAsJsonAsync($"/api/v1/catalog/categories/{categoryId}/attributes", new
        {
            attributeId,
            required = true,
            marketplaceRequired = false,
            sortOrder = 0,
        })).StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicate = await Client.PostAsJsonAsync($"/api/v1/catalog/categories/{categoryId}/attributes", new
        {
            attributeId,
            required = false,
            marketplaceRequired = false,
            sortOrder = 1,
        });

        await CatalogHttpAssertions.AssertProblemAsync(duplicate, HttpStatusCode.Conflict, "conflict");

        await Client.DeleteAsync($"/api/v1/catalog/attributes/{attributeId}");
        await Client.DeleteAsync($"/api/v1/catalog/categories/{categoryId}");
    }

    [SkippableFact]
    public async Task ListCategories_WithPagination_ReturnsPagedResult()
    {
        var createResponse = await Client.PostAsJsonAsync("/api/v1/catalog/categories", new
        {
            name = $"Paged {Guid.NewGuid():N}",
            parent_id = (Guid?)null,
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        var listResponse = await Client.GetAsync("/api/v1/catalog/categories?page=1&page_size=10");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResultResponse<CategoryResponse>>();
        page.Should().NotBeNull();
        page!.Page.Should().Be(1);
        page.PageSize.Should().Be(10);
        page.Items.Should().HaveCountLessThanOrEqualTo(10);

        var getResponse = await Client.GetAsync($"/api/v1/catalog/categories/{created!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await Client.DeleteAsync($"/api/v1/catalog/categories/{created.Id}");
    }

    [SkippableFact]
    public async Task ListCategories_WithoutPagination_UsesDefaults()
    {
        var response = await Client.GetAsync("/api/v1/catalog/categories");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResultResponse<CategoryResponse>>();
        page.Should().NotBeNull();
        page!.Page.Should().Be(1);
        page.PageSize.Should().Be(20);
    }

    [SkippableFact]
    public async Task ProductsBatch_EmptyProducts_Returns400()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/catalog/products:batch", new
        {
            group_id = Guid.NewGuid(),
            products = Array.Empty<object>(),
        });

        await CatalogHttpAssertions.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");
    }

    [SkippableFact]
    public async Task ProductsBatch_DuplicateBarcodeInRequest_Returns409()
    {
        var barcode = (8890000000L + Random.Shared.Next(1, 100000)).ToString(CultureInfo.InvariantCulture);
        var response = await Client.PostAsJsonAsync("/api/v1/catalog/products:batch", new
        {
            group_id = Guid.NewGuid(),
            products = new[]
            {
                new
                {
                    model_code = $"SKU-{Guid.NewGuid():N}",
                    name = "Dup Batch",
                    status = "draft",
                    attribute_values = Array.Empty<object>(),
                    variants = Array.Empty<object>(),
                    items = new object[]
                    {
                        new { barcode, price = 10m, stock = 1 },
                        new { barcode, price = 11m, stock = 2 },
                    },
                },
            },
        });

        await CatalogHttpAssertions.AssertProblemAsync(response, HttpStatusCode.Conflict, "conflict");
    }
}
