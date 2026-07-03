using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Catalog.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace Catalog.IntegrationTests;

/// <summary>Barkod serisi API uç noktaları için entegrasyon testleri.</summary>
public class BarcodesApiTests(CatalogPostgresFixture fixture) : CatalogIntegrationTestBase(fixture)
{
    [SkippableFact]
    public async Task BarcodeSequence_GetAndUpdate_HappyPath()
    {
        var getResponse = await Client.GetAsync("/api/v1/catalog/barcode-sequence");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var current = await getResponse.Content.ReadFromJsonAsync<BarcodeSequenceResponse>();
        current.Should().NotBeNull();
        current!.NextValue.Should().BeGreaterThan(0);

        var nextValue = current.NextValue + 1000;
        var putResponse = await Client.PutAsJsonAsync("/api/v1/catalog/barcode-sequence", new
        {
            next_value = nextValue,
            client_allocation_required = false,
        });

        putResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await putResponse.Content.ReadFromJsonAsync<BarcodeSequenceResponse>();
        updated!.NextValue.Should().Be(nextValue);
        updated.NextPreview.Should().Be(nextValue.ToString(CultureInfo.InvariantCulture));
    }

    [SkippableFact]
    public async Task BarcodeSequence_UpdateToConflictingValue_Returns409()
    {
        var baseValue = 9700000000L + Random.Shared.Next(1, 100000);
        await SetSequenceAsync(baseValue);

        var allocateResponse = await Client.PostAsJsonAsync("/api/v1/catalog/barcodes:allocate", new { count = 1 });
        allocateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var putResponse = await Client.PutAsJsonAsync("/api/v1/catalog/barcode-sequence", new
        {
            next_value = baseValue,
            client_allocation_required = false,
        });

        putResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [SkippableFact]
    public async Task Barcodes_AllocateSingleAndBatch_ReturnsNumericBarcodes()
    {
        var baseValue = 9800000000L + Random.Shared.Next(1, 100000);
        await SetSequenceAsync(baseValue);

        var singleResponse = await Client.PostAsJsonAsync("/api/v1/catalog/barcodes:allocate", new { count = 1 });
        singleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var single = await singleResponse.Content.ReadFromJsonAsync<AllocateBarcodesResponse>();
        single!.Barcodes.Should().Equal(baseValue.ToString(CultureInfo.InvariantCulture));

        var batchResponse = await Client.PostAsJsonAsync("/api/v1/catalog/barcodes:allocate", new { count = 2 });
        batchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var batch = await batchResponse.Content.ReadFromJsonAsync<AllocateBarcodesResponse>();
        batch!.Barcodes.Should().Equal(
            (baseValue + 1).ToString(CultureInfo.InvariantCulture),
            (baseValue + 2).ToString(CultureInfo.InvariantCulture));

        var listResponse = await Client.GetAsync("/api/v1/catalog/barcode-allocations?page=1&page_size=10");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task AllocateThenCreateProduct_UsesIndependentFlows()
    {
        var baseValue = 9900000000L + Random.Shared.Next(1, 100000);
        await SetSequenceAsync(baseValue);

        var allocateResponse = await Client.PostAsJsonAsync("/api/v1/catalog/barcodes:allocate", new { count = 1 });
        var allocated = await allocateResponse.Content.ReadFromJsonAsync<AllocateBarcodesResponse>();
        var barcode = allocated!.Barcodes.Single();

        var categoryId = await CreateCategoryAsync();
        var createResponse = await Client.PostAsJsonAsync("/api/v1/catalog/products", new
        {
            group_id = Guid.NewGuid(),
            category_id = categoryId,
            model_code = $"INDEP-{Guid.NewGuid():N}",
            name = "Independent Flow Product",
            status = "draft",
            attribute_values = Array.Empty<object>(),
            variants = Array.Empty<object>(),
            items = new[]
            {
                new { barcode, price = 19.99m, stock = 5 },
            },
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<AllocatedBarcodeProductResponse>();
        created!.Items[0].Barcode.Should().Be(barcode);

        await Client.DeleteAsync($"/api/v1/catalog/products/{created.Id}");
        await Client.DeleteAsync($"/api/v1/catalog/categories/{categoryId}");
    }

    private async Task SetSequenceAsync(long nextValue, bool clientAllocationRequired = false)
    {
        var response = await Client.PutAsJsonAsync("/api/v1/catalog/barcode-sequence", new
        {
            next_value = nextValue,
            client_allocation_required = clientAllocationRequired,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

/// <summary>Barkod sırası API yanıtını deserialize etmek için kullanılan DTO.</summary>
internal sealed record BarcodeSequenceResponse(
    [property: System.Text.Json.Serialization.JsonPropertyName("next_value")] long NextValue,
    [property: System.Text.Json.Serialization.JsonPropertyName("client_allocation_required")] bool ClientAllocationRequired,
    [property: System.Text.Json.Serialization.JsonPropertyName("next_preview")] string NextPreview);

/// <summary>Toplu barkod tahsis API yanıtını deserialize etmek için kullanılan DTO.</summary>
internal sealed record AllocateBarcodesResponse(IReadOnlyList<string> Barcodes);

/// <summary>Tahsis edilen barkodlu ürün API yanıtını deserialize etmek için kullanılan DTO.</summary>
internal sealed record AllocatedBarcodeProductResponse(
    Guid Id,
    IReadOnlyList<AllocatedBarcodeItemResponse> Items);

/// <summary>Tahsis edilen ürün kalemi barkod API yanıtını deserialize etmek için kullanılan DTO.</summary>
internal sealed record AllocatedBarcodeItemResponse(string Barcode);
