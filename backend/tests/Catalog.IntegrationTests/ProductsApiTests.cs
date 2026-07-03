using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Catalog.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace Catalog.IntegrationTests;

/// <summary>Products API uç noktaları için entegrasyon testleri.</summary>
public class ProductsApiTests(CatalogPostgresFixture fixture) : CatalogIntegrationTestBase(fixture)
{
    [SkippableFact]
    public async Task ProductCrud_HappyPath()
    {
        var groupId = Guid.NewGuid();
        var categoryId = await CreateCategoryAsync();

        var createResponse = await Client.PostAsJsonAsync("/api/v1/catalog/products", new
        {
            group_id = groupId,
            category_id = categoryId,
            model_code = $"INT-{Guid.NewGuid():N}",
            name = "Integration Product",
            status = "draft",
            attribute_values = Array.Empty<object>(),
            variants = Array.Empty<object>(),
            items = new[]
            {
                new
                {
                    barcode = NextNumericBarcode(),
                    price = 19.99m,
                    stock = 10,
                },
            },
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();
        created.Should().NotBeNull();
        created!.Name.Should().Be("Integration Product");
        created.CategoryId.Should().Be(categoryId);
        created.Items.Should().HaveCount(1);

        var getResponse = await Client.GetAsync($"/api/v1/catalog/products/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var patchResponse = await Client.PatchAsJsonAsync($"/api/v1/catalog/products/{created.Id}", new
        {
            category_id = categoryId,
            name = "Updated Product",
            status = "active",
            attribute_values = Array.Empty<object>(),
        });
        patchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var itemId = created.Items[0].Id;
        var itemGetResponse = await Client.GetAsync($"/api/v1/catalog/items/{itemId}");
        itemGetResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var itemPatchResponse = await Client.PatchAsJsonAsync($"/api/v1/catalog/items/{itemId}", new
        {
            price = 24.99m,
            stock = 5,
        });
        itemPatchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteResponse = await Client.DeleteAsync($"/api/v1/catalog/products/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await Client.DeleteAsync($"/api/v1/catalog/categories/{categoryId}");
    }

    [SkippableFact]
    public async Task CreateVariantProduct_WithColorAndSize_Succeeds()
    {
        var categoryId = await CreateCategoryAsync();
        var colorVariant = await CreateVariantType($"Color-{Guid.NewGuid():N}");
        var sizeVariant = await CreateVariantType($"Size-{Guid.NewGuid():N}");
        var red = await CreateVariantValue(colorVariant.Id, "Red", "#ff0000");
        var small = await CreateVariantValue(sizeVariant.Id, "S", null);

        var createResponse = await Client.PostAsJsonAsync("/api/v1/catalog/products", new
        {
            group_id = Guid.NewGuid(),
            category_id = categoryId,
            model_code = $"VAR-{Guid.NewGuid():N}",
            name = "Variant Shirt",
            status = "draft",
            attribute_values = Array.Empty<object>(),
            variants = new object[]
            {
                new { id = colorVariant.Id, name = colorVariant.Name, selection_style = "color" },
                new { id = sizeVariant.Id, name = sizeVariant.Name, selection_style = "list" },
            },
            items = new[]
            {
                new
                {
                    barcode = NextNumericBarcode(),
                    price = 19.99m,
                    stock = 5,
                    variant_values = new object[]
                    {
                        new { variant_id = colorVariant.Id, variant_value_id = red.Id },
                        new { variant_id = sizeVariant.Id, variant_value_id = small.Id },
                    },
                },
            },
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();
        product!.Items.Should().HaveCount(1);

        await Client.DeleteAsync($"/api/v1/catalog/products/{product.Id}");
        await Client.DeleteAsync($"/api/v1/catalog/categories/{categoryId}");
        await Client.DeleteAsync($"/api/v1/catalog/variant-values/{red.Id}");
        await Client.DeleteAsync($"/api/v1/catalog/variant-values/{small.Id}");
        await Client.DeleteAsync($"/api/v1/catalog/variants/{colorVariant.Id}");
        await Client.DeleteAsync($"/api/v1/catalog/variants/{sizeVariant.Id}");
    }

    [SkippableFact]
    public async Task CreateProduct_WithAttributeValues_Succeeds()
    {
        var categoryId = await CreateCategoryAsync();
        var attributeResponse = await Client.PostAsJsonAsync("/api/v1/catalog/attributes", new
        {
            name = $"Material {Guid.NewGuid():N}",
        });
        attributeResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var attribute = await attributeResponse.Content.ReadFromJsonAsync<AttributeResponse>();

        var valueResponse = await Client.PostAsJsonAsync($"/api/v1/catalog/attributes/{attribute!.Id}/values", new { name = "Cotton" });
        valueResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var value = await valueResponse.Content.ReadFromJsonAsync<AttributeValueResponse>();

        var createResponse = await Client.PostAsJsonAsync("/api/v1/catalog/products", new
        {
            group_id = Guid.NewGuid(),
            category_id = categoryId,
            model_code = $"ATTR-{Guid.NewGuid():N}",
            name = "Attributed Product",
            status = "draft",
            attribute_values = new[]
            {
                new { attribute_id = attribute.Id, attribute_value_id = value!.Id },
            },
            variants = Array.Empty<object>(),
            items = new[] { new { barcode = NextNumericBarcode(), price = 10m, stock = 1 } },
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var product = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();
        await Client.DeleteAsync($"/api/v1/catalog/products/{product!.Id}");
        await Client.DeleteAsync($"/api/v1/catalog/categories/{categoryId}");
        await Client.DeleteAsync($"/api/v1/catalog/attributes/{attribute.Id}");
    }

    [SkippableFact]
    public async Task DeleteProductItem_FromVariantProduct_Succeeds()
    {
        var categoryId = await CreateCategoryAsync();
        var sizeVariant = await CreateVariantType($"Size-{Guid.NewGuid():N}");
        var small = await CreateVariantValue(sizeVariant.Id, "S", null);
        var medium = await CreateVariantValue(sizeVariant.Id, "M", null);

        var createResponse = await Client.PostAsJsonAsync("/api/v1/catalog/products", new
        {
            group_id = Guid.NewGuid(),
            category_id = categoryId,
            model_code = $"DEL-{Guid.NewGuid():N}",
            name = "Deletable Items",
            status = "draft",
            attribute_values = Array.Empty<object>(),
            variants = new[] { new { id = sizeVariant.Id, name = sizeVariant.Name, selection_style = "list" } },
            items = new object[]
            {
                new
                {
                    barcode = NextNumericBarcode(),
                    price = 10m,
                    stock = 1,
                    variant_values = new[] { new { variant_id = sizeVariant.Id, variant_value_id = small.Id } },
                },
                new
                {
                    barcode = NextNumericBarcode(),
                    price = 11m,
                    stock = 2,
                    variant_values = new[] { new { variant_id = sizeVariant.Id, variant_value_id = medium.Id } },
                },
            },
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();
        var itemToDelete = product!.Items[0].Id;

        var deleteItemResponse = await Client.DeleteAsync($"/api/v1/catalog/items/{itemToDelete}");
        deleteItemResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getProduct = await Client.GetAsync($"/api/v1/catalog/products/{product.Id}");
        var updated = await getProduct.Content.ReadFromJsonAsync<ProductResponse>();
        updated!.Items.Should().HaveCount(1);

        await Client.DeleteAsync($"/api/v1/catalog/products/{product.Id}");
        await Client.DeleteAsync($"/api/v1/catalog/categories/{categoryId}");
        await Client.DeleteAsync($"/api/v1/catalog/variant-values/{small.Id}");
        await Client.DeleteAsync($"/api/v1/catalog/variant-values/{medium.Id}");
        await Client.DeleteAsync($"/api/v1/catalog/variants/{sizeVariant.Id}");
    }

    private async Task<VariantResponse> CreateVariantType(string name)
    {
        var response = await Client.PostAsJsonAsync("/api/v1/catalog/variants", new
        {
            name,
            selectionStyle = "list",
            sortOrder = 0,
            slicer = false,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<VariantResponse>())!;
    }

    private async Task<VariantValueResponse> CreateVariantValue(Guid typeId, string label, string? color)
    {
        var response = await Client.PostAsJsonAsync($"/api/v1/catalog/variants/{typeId}/values", new
        {
            label,
            color,
            sortOrder = 0,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<VariantValueResponse>())!;
    }

    private static string NextNumericBarcode() =>
        (9100000000L + Random.Shared.Next(1, 1000000)).ToString(CultureInfo.InvariantCulture);
}

/// <summary>API ürün yanıtını deserialize etmek için kullanılan DTO.</summary>
internal sealed record ProductResponse(
    Guid Id,
    Guid GroupId,
    Guid CategoryId,
    string ModelCode,
    string Name,
    string Status,
    JsonElement AttributeValues,
    JsonElement Variants,
    IReadOnlyList<ItemResponse> Items);

/// <summary>API ürün kalemi yanıtını deserialize etmek için kullanılan DTO.</summary>
internal sealed record ItemResponse(Guid Id, string Barcode, decimal Price, int Stock);
