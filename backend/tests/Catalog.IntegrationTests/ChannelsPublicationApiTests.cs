using System.Net;
using System.Net.Http.Json;
using Catalog.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace Catalog.IntegrationTests;

/// <summary>Ürün yayın (publication) enqueue + durum sorgu API'si için entegrasyon testleri.</summary>
public class ChannelsPublicationApiTests(CatalogPostgresFixture fixture) : CatalogIntegrationTestBase(fixture)
{
    [SkippableFact]
    public async Task EnqueuePublication_WithConnection_CreatesPendingRun()
    {
        await UpsertTrendyolConnectionAsync();

        var response = await Client.PostAsync("/api/v1/channels/marketplaces/TY/publications", null);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var run = (await response.Content.ReadFromJsonAsync<PublicationRunResponse>(CatalogJson.Options))!;
        run.Status.Should().Be("pending");
        run.MarketplaceCode.Should().Be("TY");

        var fetched = await Client.GetFromJsonAsync<PublicationRunResponse>(
            $"/api/v1/channels/marketplaces/TY/publications/{run.Id}", CatalogJson.Options);
        fetched!.Id.Should().Be(run.Id);
        fetched.Status.Should().Be("pending");
        fetched.PublishedItems.Should().Be(0);
    }

    [SkippableFact]
    public async Task EnqueuePublication_WithoutConnection_ReturnsNotFound()
    {
        var response = await Client.PostAsync("/api/v1/channels/marketplaces/TY/publications", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [SkippableFact]
    public async Task EnqueuePublication_WhenActiveRunExists_ReturnsConflict()
    {
        await UpsertTrendyolConnectionAsync();

        var first = await Client.PostAsync("/api/v1/channels/marketplaces/TY/publications", null);
        first.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var second = await Client.PostAsync("/api/v1/channels/marketplaces/TY/publications", null);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private async Task UpsertTrendyolConnectionAsync()
    {
        var response = await Client.PutAsJsonAsync("/api/v1/channels/marketplaces/TY/connection", new
        {
            seller_id = "seller-123",
            api_key = "test-api-key",
            api_secret = "test-secret",
            is_enabled = true,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record PublicationRunResponse(
        Guid Id,
        string MarketplaceCode,
        string Status,
        int ProcessedItems,
        int PublishedItems,
        int FailedItems);
}
