using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Catalog.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace Catalog.IntegrationTests;

/// <summary>Toplu ürün oluşturma API uç noktası için entegrasyon testleri.</summary>
public class ProductsBatchApiTests(CatalogPostgresFixture fixture) : CatalogIntegrationTestBase(fixture)
{
    [SkippableFact]
    public async Task ProductsBatch_WithSlicer_SplitsIntoMultipleProducts()
    {
        var categoryId = await CreateCategoryAsync();
        var colorVariant = await CreateVariant($"Color-{Guid.NewGuid():N}", slicer: true);
        var sizeVariant = await CreateVariant($"Size-{Guid.NewGuid():N}", slicer: false);
        var red = await CreateVariantValue(colorVariant.Id, "Red", "#ff0000");
        var blue = await CreateVariantValue(colorVariant.Id, "Blue", "#0000ff");
        var small = await CreateVariantValue(sizeVariant.Id, "S", null);
        var medium = await CreateVariantValue(sizeVariant.Id, "M", null);
        var groupId = Guid.NewGuid();
        var baseSku = $"BATCH-{Guid.NewGuid():N}";

        var batchResponse = await Client.PostAsJsonAsync("/api/v1/catalog/products:batch", new
        {
            group_id = groupId,
            products = new[]
            {
                new
                {
                    category_id = categoryId,
                    model_code = baseSku,
                    name = "Batch Shirt",
                    status = "draft",
                    attribute_values = Array.Empty<object>(),
                    variants = new[]
                    {
                        new { id = colorVariant.Id, name = colorVariant.Name, selection_style = "color" },
                        new { id = sizeVariant.Id, name = sizeVariant.Name, selection_style = "list" },
                    },
                    items = new object[]
                    {
                        Item(red, colorVariant, small, sizeVariant),
                        Item(red, colorVariant, medium, sizeVariant),
                        Item(blue, colorVariant, small, sizeVariant),
                        Item(blue, colorVariant, medium, sizeVariant),
                    },
                },
            },
        });

        batchResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var batchResult = await batchResponse.Content.ReadFromJsonAsync<BatchCreateResponse>();
        batchResult.Should().NotBeNull();
        batchResult!.Products.Should().HaveCount(2);
        batchResult.Products.Should().OnlyContain(p => p.Name.StartsWith("Batch Shirt - "));
        batchResult.Products.Should().OnlyContain(p => p.ModelCode.StartsWith($"{baseSku}-"));
        batchResult.Products.SelectMany(p => p.Items).Should().HaveCount(4);

        foreach (var product in batchResult.Products)
        {
            await Client.DeleteAsync($"/api/v1/catalog/products/{product.Id}");
        }

        await Client.DeleteAsync($"/api/v1/catalog/variant-values/{red.Id}");
        await Client.DeleteAsync($"/api/v1/catalog/variant-values/{blue.Id}");
        await Client.DeleteAsync($"/api/v1/catalog/variant-values/{small.Id}");
        await Client.DeleteAsync($"/api/v1/catalog/variant-values/{medium.Id}");
        await Client.DeleteAsync($"/api/v1/catalog/variants/{colorVariant.Id}");
        await Client.DeleteAsync($"/api/v1/catalog/variants/{sizeVariant.Id}");
        await Client.DeleteAsync($"/api/v1/catalog/categories/{categoryId}");
    }

    [SkippableFact]
    public async Task ProductsBatch_WithoutSlicer_CreatesSingleProduct()
    {
        var categoryId = await CreateCategoryAsync();
        var sizeVariant = await CreateVariant($"Size-{Guid.NewGuid():N}", slicer: false);
        var small = await CreateVariantValue(sizeVariant.Id, "S", null);
        var modelCode = $"BATCH-SINGLE-{Guid.NewGuid():N}";

        var batchResponse = await Client.PostAsJsonAsync("/api/v1/catalog/products:batch", new
        {
            group_id = Guid.NewGuid(),
            products = new[]
            {
                new
                {
                    category_id = categoryId,
                    model_code = modelCode,
                    name = "Single Batch Shirt",
                    status = "draft",
                    attribute_values = Array.Empty<object>(),
                    variants = new[] { new { id = sizeVariant.Id, name = sizeVariant.Name, selection_style = "list" } },
                    items = new object[]
                    {
                        new
                        {
                            barcode = NextNumericBarcode(),
                            price = 19.99m,
                            stock = 10,
                            variant_values = new[] { new { variant_id = sizeVariant.Id, variant_value_id = small.Id } },
                        },
                    },
                },
            },
        });

        batchResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var batchResult = await batchResponse.Content.ReadFromJsonAsync<BatchCreateResponse>();
        batchResult!.Products.Should().ContainSingle();
        batchResult.Products[0].ModelCode.Should().Be(modelCode);

        await Client.DeleteAsync($"/api/v1/catalog/products/{batchResult.Products[0].Id}");
        await Client.DeleteAsync($"/api/v1/catalog/variant-values/{small.Id}");
        await Client.DeleteAsync($"/api/v1/catalog/variants/{sizeVariant.Id}");
        await Client.DeleteAsync($"/api/v1/catalog/categories/{categoryId}");
    }

    [SkippableFact]
    public async Task ProductsBatch_WithSlicerOnly_SplitsByColor()
    {
        var categoryId = await CreateCategoryAsync();
        var colorVariant = await CreateVariant($"Color-{Guid.NewGuid():N}", slicer: true);
        var red = await CreateVariantValue(colorVariant.Id, "Red", "#ff0000");
        var blue = await CreateVariantValue(colorVariant.Id, "Blue", "#0000ff");
        var baseSku = $"BATCH-COLOR-{Guid.NewGuid():N}";

        var batchResponse = await Client.PostAsJsonAsync("/api/v1/catalog/products:batch", new
        {
            group_id = Guid.NewGuid(),
            products = new[]
            {
                new
                {
                    category_id = categoryId,
                    model_code = baseSku,
                    name = "Color Only Shirt",
                    status = "draft",
                    attribute_values = Array.Empty<object>(),
                    variants = new[] { new { id = colorVariant.Id, name = colorVariant.Name, selection_style = "color" } },
                    items = new object[]
                    {
                        new
                        {
                            barcode = NextNumericBarcode(),
                            price = 19.99m,
                            stock = 10,
                            variant_values = new[] { new { variant_id = colorVariant.Id, variant_value_id = red.Id } },
                        },
                        new
                        {
                            barcode = NextNumericBarcode(),
                            price = 19.99m,
                            stock = 10,
                            variant_values = new[] { new { variant_id = colorVariant.Id, variant_value_id = blue.Id } },
                        },
                    },
                },
            },
        });

        batchResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var batchResult = await batchResponse.Content.ReadFromJsonAsync<BatchCreateResponse>();
        batchResult!.Products.Should().HaveCount(2);
        batchResult.Products.Should().OnlyContain(p => p.Name.StartsWith("Color Only Shirt - "));

        foreach (var product in batchResult.Products)
        {
            await Client.DeleteAsync($"/api/v1/catalog/products/{product.Id}");
        }

        await Client.DeleteAsync($"/api/v1/catalog/variant-values/{red.Id}");
        await Client.DeleteAsync($"/api/v1/catalog/variant-values/{blue.Id}");
        await Client.DeleteAsync($"/api/v1/catalog/variants/{colorVariant.Id}");
        await Client.DeleteAsync($"/api/v1/catalog/categories/{categoryId}");
    }

    private async Task<VariantResponse> CreateVariant(string name, bool slicer)
    {
        var response = await Client.PostAsJsonAsync("/api/v1/catalog/variants", new
        {
            name,
            selectionStyle = "list",
            sortOrder = 0,
            slicer,
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

    private static object Item(
        VariantValueResponse colorValue,
        VariantResponse colorVariant,
        VariantValueResponse sizeValue,
        VariantResponse sizeVariant) =>
        new
        {
            barcode = NextNumericBarcode(),
            price = 19.99m,
            stock = 10,
            variant_values = new object[]
            {
                new
                {
                    variant_id = colorVariant.Id,
                    variant_value_id = colorValue.Id,
                },
                new
                {
                    variant_id = sizeVariant.Id,
                    variant_value_id = sizeValue.Id,
                },
            },
        };

    private static string NextNumericBarcode() =>
        (9200000000L + Random.Shared.Next(1, 1000000)).ToString(CultureInfo.InvariantCulture);
}

/// <summary>Toplu ürün oluşturma API yanıtını deserialize etmek için kullanılan DTO.</summary>
internal sealed record BatchCreateResponse(IReadOnlyList<BatchProductResponse> Products);

/// <summary>Toplu oluşturulan ürün API yanıtını deserialize etmek için kullanılan DTO.</summary>
internal sealed record BatchProductResponse(
    Guid Id,
    [property: System.Text.Json.Serialization.JsonPropertyName("modelCode")] string ModelCode,
    string Name,
    IReadOnlyList<BatchItemResponse> Items);

/// <summary>Toplu oluşturulan ürün kalemi API yanıtını deserialize etmek için kullanılan DTO.</summary>
internal sealed record BatchItemResponse(Guid Id, string Barcode);
