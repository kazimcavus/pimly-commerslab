using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Catalog.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace Catalog.IntegrationTests;

/// <summary>SKU oluşturucu ile toplu ürün oluşturma entegrasyon testleri.</summary>
public class SkuGeneratorBatchApiTests(CatalogPostgresFixture fixture) : CatalogIntegrationTestBase(fixture)
{
    [SkippableFact]
    public async Task ProductsBatch_WithSkuGenerator_GeneratesModelCodeAndVariantSku()
    {
        await EnableSkuGeneratorAsync();

        var categoryId = await CreateCategoryAsync();
        var colorVariant = await CreateVariantType("Color", "color", slicer: false);
        var sizeVariant = await CreateVariantType("Size", "list", slicer: false);
        var red = await CreateVariantValue(colorVariant.Id, "Red", "#ff0000", "R08");
        var medium = await CreateVariantValue(sizeVariant.Id, "M", null, "M");

        var batchResponse = await Client.PostAsJsonAsync("/api/v1/catalog/products:batch", new
        {
            group_id = Guid.NewGuid(),
            products = new[]
            {
                new
                {
                    category_id = categoryId,
                    model_code = string.Empty,
                    code_inputs = Array.Empty<string>(),
                    name = "Generated Shirt",
                    status = "draft",
                    attribute_values = Array.Empty<object>(),
                    variants = new[]
                    {
                        new { id = colorVariant.Id, name = colorVariant.Name, selection_style = "color" },
                        new { id = sizeVariant.Id, name = sizeVariant.Name, selection_style = "list" },
                    },
                    items = new object[]
                    {
                        new
                        {
                            sku = (string?)null,
                            barcode = NextNumericBarcode(),
                            price = 19.99m,
                            stock = 10,
                            variant_values = new object[]
                            {
                                new { variant_id = colorVariant.Id, variant_value_id = red.Id },
                                new { variant_id = sizeVariant.Id, variant_value_id = medium.Id },
                            },
                        },
                    },
                },
            },
        });

        batchResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var batchResult = await batchResponse.Content.ReadFromJsonAsync<SkuBatchCreateResponse>();
        batchResult!.Products.Should().ContainSingle();
        batchResult.Products[0].ModelCode.Should().Be("261000");
        batchResult.Products[0].Items.Should().ContainSingle();
        batchResult.Products[0].Items[0].Sku.Should().Be("261000R08M");

        await Client.DeleteAsync($"/api/v1/catalog/products/{batchResult.Products[0].Id}");
        await Client.DeleteAsync($"/api/v1/catalog/variant-values/{red.Id}");
        await Client.DeleteAsync($"/api/v1/catalog/variant-values/{medium.Id}");
        await Client.DeleteAsync($"/api/v1/catalog/variants/{colorVariant.Id}");
        await Client.DeleteAsync($"/api/v1/catalog/variants/{sizeVariant.Id}");
        await Client.DeleteAsync($"/api/v1/catalog/categories/{categoryId}");

        await DisableSkuGeneratorAsync();
    }

    private async Task EnableSkuGeneratorAsync()
    {
        var response = await Client.PutAsJsonAsync("/api/v1/catalog/sku-config", new
        {
            enabled = true,
            segments = new object[]
            {
                new { type = "fixed", value = "26" },
                new { type = "counter", start = 1000, width = 4 },
                new { type = "color", source = "code" },
                new { type = "size", source = "code" },
            },
            counter_next_value = 1000L,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task DisableSkuGeneratorAsync()
    {
        await Client.PutAsJsonAsync("/api/v1/catalog/sku-config", new
        {
            enabled = false,
            segments = Array.Empty<object>(),
        });
    }

    private async Task<VariantResponse> CreateVariantType(string name, string selectionStyle, bool slicer)
    {
        var response = await Client.PostAsJsonAsync("/api/v1/catalog/variants", new
        {
            name,
            selectionStyle,
            sortOrder = 0,
            slicer,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<VariantResponse>())!;
    }

    private async Task<VariantValueResponse> CreateVariantValue(
        Guid typeId,
        string label,
        string? color,
        string key)
    {
        var response = await Client.PostAsJsonAsync($"/api/v1/catalog/variants/{typeId}/values", new
        {
            label,
            color,
            key,
            sortOrder = 0,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<VariantValueResponse>())!;
    }

    private static string NextNumericBarcode() =>
        (9300000000L + Random.Shared.Next(1, 1000000)).ToString(CultureInfo.InvariantCulture);
}

/// <summary>SKU üretici ile toplu ürün oluşturma API yanıtını deserialize etmek için kullanılan DTO.</summary>
internal sealed record SkuBatchCreateResponse(IReadOnlyList<SkuBatchProductResponse> Products);

/// <summary>SKU batch ile oluşturulan ürün API yanıtını deserialize etmek için kullanılan DTO.</summary>
internal sealed record SkuBatchProductResponse(
    Guid Id,
    [property: JsonPropertyName("model_code")] string ModelCode,
    string Name,
    IReadOnlyList<SkuBatchItemResponse> Items);

/// <summary>SKU batch ile oluşturulan ürün kalemi API yanıtını deserialize etmek için kullanılan DTO.</summary>
internal sealed record SkuBatchItemResponse(
    Guid Id,
    string Barcode,
    string? Sku);
