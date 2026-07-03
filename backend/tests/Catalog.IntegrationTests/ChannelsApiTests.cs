using System.Net;
using System.Net.Http.Json;
using Catalog.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace Catalog.IntegrationTests;

/// <summary>Channels modülü API uç noktaları için entegrasyon testleri.</summary>
public class ChannelsApiTests(CatalogPostgresFixture fixture) : CatalogIntegrationTestBase(fixture)
{
    [SkippableFact]
    public async Task ListMarketplaces_ReturnsSeededTrendyol()
    {
        var response = await Client.GetAsync("/api/v1/channels/marketplaces");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var marketplaces = await response.Content.ReadFromJsonAsync<List<MarketplaceResponse>>();
        marketplaces.Should().NotBeNull();
        marketplaces!.Should().ContainSingle(m =>
            m.Code == "TY"
            && m.Name == "Trendyol"
            && m.IsActive
            && !m.IsConfigured);
    }

    [SkippableFact]
    public async Task UpsertAndGetConnection_HappyPath()
    {
        const string apiKey = "test-api-key-12345678";

        var missingResponse = await Client.GetAsync("/api/v1/channels/marketplaces/TY/connection");
        await CatalogHttpAssertions.AssertProblemAsync(missingResponse, HttpStatusCode.NotFound, "not_found");

        var upsertResponse = await Client.PutAsJsonAsync("/api/v1/channels/marketplaces/TY/connection", new
        {
            seller_id = "seller-123",
            api_key = apiKey,
            api_secret = "secret-value",
            is_enabled = true,
        });
        upsertResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var upserted = await upsertResponse.Content.ReadFromJsonAsync<MarketplaceConnectionResponse>();
        upserted.Should().NotBeNull();
        upserted!.MarketplaceCode.Should().Be("TY");
        upserted.SellerId.Should().Be("seller-123");
        upserted.HasApiKey.Should().BeTrue();
        upserted.HasApiSecret.Should().BeTrue();
        upserted.ApiKeyHint.Should().Be("5678");
        upserted.IsEnabled.Should().BeTrue();

        var getResponse = await Client.GetAsync("/api/v1/channels/marketplaces/TY/connection");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await getResponse.Content.ReadFromJsonAsync<MarketplaceConnectionResponse>();
        fetched!.Id.Should().Be(upserted.Id);
        fetched.ApiKeyHint.Should().Be("5678");

        var listResponse = await Client.GetAsync("/api/v1/channels/marketplaces");
        var marketplaces = await listResponse.Content.ReadFromJsonAsync<List<MarketplaceResponse>>();
        marketplaces!.Should().ContainSingle(m => m.Code == "TY" && m.IsConfigured);
    }

    [SkippableFact]
    public async Task UpsertConnection_MissingApiKey_ReturnsValidationError()
    {
        var response = await Client.PutAsJsonAsync("/api/v1/channels/marketplaces/TY/connection", new
        {
            seller_id = "seller-123",
            api_key = string.Empty,
            api_secret = (string?)null,
            is_enabled = true,
        });

        await CatalogHttpAssertions.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");
    }

    [SkippableFact]
    public async Task GetConnection_UnknownMarketplace_ReturnsNotFound()
    {
        var response = await Client.GetAsync("/api/v1/channels/marketplaces/unknown-mp/connection");
        await CatalogHttpAssertions.AssertProblemAsync(response, HttpStatusCode.NotFound, "not_found");
    }

    private sealed record MarketplaceResponse(
        string Code,
        string Name,
        bool IsActive,
        bool IsConfigured);

    private sealed record MarketplaceConnectionResponse(
        Guid Id,
        string MarketplaceCode,
        string? SellerId,
        bool HasApiKey,
        bool HasApiSecret,
        string? ApiKeyHint,
        bool IsEnabled);
}
