using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Catalog.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace Catalog.IntegrationTests;

/// <summary>Products API hata yolu E2E testleri.</summary>
public class ProductsApiValidationTests(CatalogPostgresFixture fixture) : CatalogIntegrationTestBase(fixture)
{
    [SkippableFact]
    public async Task GetProduct_UnknownId_Returns404()
    {
        var response = await Client.GetAsync($"/api/v1/catalog/products/{Guid.NewGuid()}");
        await CatalogHttpAssertions.AssertProblemAsync(response, HttpStatusCode.NotFound, "not_found");
    }

    [SkippableFact]
    public async Task CreateProduct_EmptyModelCode_Returns400()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/catalog/products", new
        {
            group_id = Guid.NewGuid(),
            model_code = "  ",
            name = "Invalid",
            status = "draft",
            attribute_values = Array.Empty<object>(),
            variants = Array.Empty<object>(),
            items = new[] { new { barcode = NextNumericBarcode(), price = 10m, stock = 1 } },
        });

        await CatalogHttpAssertions.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");
    }

    [SkippableFact]
    public async Task CreateProduct_EmptyItems_Returns400()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/catalog/products", new
        {
            group_id = Guid.NewGuid(),
            model_code = $"SKU-{Guid.NewGuid():N}",
            name = "Invalid",
            status = "draft",
            attribute_values = Array.Empty<object>(),
            variants = Array.Empty<object>(),
            items = Array.Empty<object>(),
        });

        await CatalogHttpAssertions.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");
    }

    [SkippableFact]
    public async Task CreateProduct_DuplicateModelCode_Returns409()
    {
        var groupId = Guid.NewGuid();
        var modelCode = $"DUP-{Guid.NewGuid():N}";
        var payload = new
        {
            group_id = groupId,
            model_code = modelCode,
            name = "First",
            status = "draft",
            attribute_values = Array.Empty<object>(),
            variants = Array.Empty<object>(),
            items = new[] { new { barcode = NextNumericBarcode(), price = 10m, stock = 1 } },
        };

        (await Client.PostAsJsonAsync("/api/v1/catalog/products", payload)).StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicate = await Client.PostAsJsonAsync("/api/v1/catalog/products", new
        {
            group_id = groupId,
            model_code = modelCode,
            name = "Second",
            status = "draft",
            attribute_values = Array.Empty<object>(),
            variants = Array.Empty<object>(),
            items = new[] { new { barcode = NextNumericBarcode(), price = 10m, stock = 1 } },
        });

        await CatalogHttpAssertions.AssertProblemAsync(duplicate, HttpStatusCode.Conflict, "conflict");
    }

    [SkippableFact]
    public async Task CreateProduct_DuplicateBarcode_Returns409()
    {
        var barcode = (8880000000L + Random.Shared.Next(1, 100000)).ToString(CultureInfo.InvariantCulture);
        var first = await Client.PostAsJsonAsync("/api/v1/catalog/products", new
        {
            group_id = Guid.NewGuid(),
            model_code = $"SKU-A-{Guid.NewGuid():N}",
            name = "First",
            status = "draft",
            attribute_values = Array.Empty<object>(),
            variants = Array.Empty<object>(),
            items = new[] { new { barcode, price = 10m, stock = 1 } },
        });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await Client.PostAsJsonAsync("/api/v1/catalog/products", new
        {
            group_id = Guid.NewGuid(),
            model_code = $"SKU-B-{Guid.NewGuid():N}",
            name = "Second",
            status = "draft",
            attribute_values = Array.Empty<object>(),
            variants = Array.Empty<object>(),
            items = new[] { new { barcode, price = 10m, stock = 1 } },
        });

        await CatalogHttpAssertions.AssertProblemAsync(second, HttpStatusCode.Conflict, "conflict");
    }

    [SkippableFact]
    public async Task CreateProduct_WithSlicerVariantType_Returns400()
    {
        var variantResponse = await Client.PostAsJsonAsync("/api/v1/catalog/variants", new
        {
            name = $"Slicer-{Guid.NewGuid():N}",
            selectionStyle = "color",
            sortOrder = 0,
            slicer = true,
        });
        variantResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var variant = await variantResponse.Content.ReadFromJsonAsync<VariantResponse>();

        var response = await Client.PostAsJsonAsync("/api/v1/catalog/products", new
        {
            group_id = Guid.NewGuid(),
            model_code = $"SKU-{Guid.NewGuid():N}",
            name = "Invalid Slicer Product",
            status = "draft",
            attribute_values = Array.Empty<object>(),
            variants = new[] { new { id = variant!.Id, name = variant.Name, selection_style = "color" } },
            items = new[] { new { barcode = NextNumericBarcode(), price = 10m, stock = 1 } },
        });

        await CatalogHttpAssertions.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");

        await Client.DeleteAsync($"/api/v1/catalog/variants/{variant.Id}");
    }

    [SkippableFact]
    public async Task PatchProduct_UnknownId_Returns404()
    {
        var response = await Client.PatchAsJsonAsync($"/api/v1/catalog/products/{Guid.NewGuid()}", new
        {
            name = "Missing",
            status = "draft",
            attribute_values = Array.Empty<object>(),
        });

        await CatalogHttpAssertions.AssertProblemAsync(response, HttpStatusCode.NotFound, "not_found");
    }

    [SkippableFact]
    public async Task DeleteProduct_UnknownId_Returns404()
    {
        var response = await Client.DeleteAsync($"/api/v1/catalog/products/{Guid.NewGuid()}");
        await CatalogHttpAssertions.AssertProblemAsync(response, HttpStatusCode.NotFound, "not_found");
    }

    [SkippableFact]
    public async Task GetProductItem_UnknownId_Returns404()
    {
        var response = await Client.GetAsync($"/api/v1/catalog/items/{Guid.NewGuid()}");
        await CatalogHttpAssertions.AssertProblemAsync(response, HttpStatusCode.NotFound, "not_found");
    }

    [SkippableFact]
    public async Task PatchProductItem_NegativePrice_Returns400()
    {
        var createResponse = await Client.PostAsJsonAsync("/api/v1/catalog/products", new
        {
            group_id = Guid.NewGuid(),
            model_code = $"SKU-{Guid.NewGuid():N}",
            name = "Item Validation",
            status = "draft",
            attribute_values = Array.Empty<object>(),
            variants = Array.Empty<object>(),
            items = new[] { new { barcode = NextNumericBarcode(), price = 10m, stock = 1 } },
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();
        var itemId = product!.Items[0].Id;

        var response = await Client.PatchAsJsonAsync($"/api/v1/catalog/items/{itemId}", new { price = -1m, stock = 1 });

        await CatalogHttpAssertions.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");

        await Client.DeleteAsync($"/api/v1/catalog/products/{product.Id}");
    }

    [SkippableFact]
    public async Task DeleteProductItem_UnknownId_Returns404()
    {
        var response = await Client.DeleteAsync($"/api/v1/catalog/items/{Guid.NewGuid()}");
        await CatalogHttpAssertions.AssertProblemAsync(response, HttpStatusCode.NotFound, "not_found");
    }

    private static string NextNumericBarcode() =>
        (9100000000L + Random.Shared.Next(1, 1000000)).ToString(CultureInfo.InvariantCulture);
}
