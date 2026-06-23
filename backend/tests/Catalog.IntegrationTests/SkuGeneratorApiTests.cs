using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Catalog.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace Catalog.IntegrationTests;

/// <summary>SKU oluşturucu yapılandırma API uç noktaları için entegrasyon testleri.</summary>
public class SkuGeneratorApiTests(CatalogPostgresFixture fixture) : CatalogIntegrationTestBase(fixture)
{
    [SkippableFact]
    public async Task SkuConfig_GetAndUpdate_HappyPath()
    {
        var getResponse = await Client.GetAsync("/api/v1/catalog/sku-config");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var current = await getResponse.Content.ReadFromJsonAsync<SkuGeneratorConfigResponse>();
        current.Should().NotBeNull();

        var putResponse = await Client.PutAsJsonAsync("/api/v1/catalog/sku-config", new
        {
            enabled = true,
            segments = new object[]
            {
                new { type = "fixed", label = "Firma", value = "26" },
                new { type = "year", label = "Yıl", digits = 2 },
                new { type = "counter", label = "No", start = 1000, width = 4 },
            },
            counter_next_value = 1000L,
        });

        putResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await putResponse.Content.ReadFromJsonAsync<SkuGeneratorConfigResponse>();
        updated!.Enabled.Should().BeTrue();
        updated.Segments.Should().HaveCount(3);
        updated.CounterNextValue.Should().Be(1000);
    }

    [SkippableFact]
    public async Task SkuConfig_UpdateCounterBelowCurrent_Returns409()
    {
        await Client.PutAsJsonAsync("/api/v1/catalog/sku-config", new
        {
            enabled = true,
            segments = new[] { new { type = "counter", start = 1000, width = 4 } },
            counter_next_value = 2000L,
        });

        var putResponse = await Client.PutAsJsonAsync("/api/v1/catalog/sku-config", new
        {
            enabled = true,
            segments = new[] { new { type = "counter", start = 1000, width = 4 } },
            counter_next_value = 1000L,
        });

        putResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}

internal sealed record SkuGeneratorConfigResponse(
    bool Enabled,
    IReadOnlyList<SkuSegmentResponse> Segments,
    [property: JsonPropertyName("counter_next_value")] long CounterNextValue);

internal sealed record SkuSegmentResponse(
    string Type,
    string? Label,
    string? Value,
    int? Start,
    int? Width,
    int? Digits,
    string? Source);
