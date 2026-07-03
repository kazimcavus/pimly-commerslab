using System.Net;
using System.Net.Http.Json;
using Catalog.IntegrationTests.Infrastructure;
using Channels.Application.Taxonomy.EnqueueTaxonomySync;
using Channels.Application.Taxonomy.ProcessTaxonomySync;
using Channels.Domain.Marketplaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.IntegrationTests;

/// <summary>Channels taxonomy sync API uç noktaları için entegrasyon testleri.</summary>
public class ChannelsTaxonomyApiTests(CatalogPostgresFixture fixture) : CatalogIntegrationTestBase(fixture)
{
    [SkippableFact]
    public async Task TaxonomySync_HappyPath()
    {
        await UpsertTrendyolConnectionAsync();

        var syncRun = await EnqueueTaxonomySyncAsync(Factory);
        syncRun.Status.Should().Be("pending");

        await ProcessPendingSyncRunsAsync(Factory);

        var getRunResponse = await Client.GetAsync(
            $"/api/v1/channels/marketplaces/trendyol/taxonomy/sync-runs/{syncRun.Id}");
        getRunResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var completedRun = await getRunResponse.Content.ReadFromJsonAsync<TaxonomySyncRunResponse>();
        completedRun!.Status.Should().Be("completed");
        completedRun.ProcessedCount.Should().BeGreaterThan(0);

        var statusResponse = await Client.GetAsync("/api/v1/channels/marketplaces/trendyol/taxonomy/status");
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var status = await statusResponse.Content.ReadFromJsonAsync<TaxonomyStatusResponse>();
        status!.CachedCategoryCount.Should().BeGreaterThan(0);
        status.IsSyncActive.Should().BeFalse();

        var searchResponse = await Client.GetAsync("/api/v1/channels/marketplaces/trendyol/categories?q=Telefon");
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var categories = await searchResponse.Content.ReadFromJsonAsync<List<ExternalCategoryResponse>>();
        categories.Should().NotBeEmpty();
        categories!.Should().Contain(category => category.Name.Contains("Telefon", StringComparison.OrdinalIgnoreCase));
    }

    [SkippableFact]
    public async Task TaxonomySync_WithoutConnection_StillWorks()
    {
        var syncRun = await EnqueueTaxonomySyncAsync(Factory);
        syncRun.Status.Should().Be("pending");

        await ProcessPendingSyncRunsAsync(Factory);

        var getRunResponse = await Client.GetAsync(
            $"/api/v1/channels/marketplaces/trendyol/taxonomy/sync-runs/{syncRun.Id}");
        getRunResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var completedRun = await getRunResponse.Content.ReadFromJsonAsync<TaxonomySyncRunResponse>();
        completedRun!.Status.Should().Be("completed");
        completedRun.ProcessedCount.Should().BeGreaterThan(0);
    }

    [SkippableFact]
    public async Task EnqueueTaxonomySync_WhenActiveRunExists_ReturnsConflict()
    {
        await UpsertTrendyolConnectionAsync();

        var first = await EnqueueTaxonomySyncAsync(Factory);
        first.Status.Should().Be("pending");

        await using var scope = Factory.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<IEnqueueTaxonomySyncHandler>();
        var marketplaceKey = MarketplaceKey.Create("trendyol").Value;
        var second = await handler.ExecuteAsync(new EnqueueTaxonomySyncCommand(marketplaceKey));

        second.IsFailure.Should().BeTrue();
        second.Error.Code.Should().Be("conflict");
    }

    private async Task UpsertTrendyolConnectionAsync()
    {
        var response = await Client.PutAsJsonAsync("/api/v1/channels/marketplaces/trendyol/connection", new
        {
            seller_id = "seller-123",
            api_key = "test-api-key",
            api_secret = "test-secret",
            is_enabled = true,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<TaxonomySyncRunResponse> EnqueueTaxonomySyncAsync(WebApplicationFactory<Program> factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<IEnqueueTaxonomySyncHandler>();
        var marketplaceKey = MarketplaceKey.Create("trendyol").Value;
        var result = await handler.ExecuteAsync(new EnqueueTaxonomySyncCommand(marketplaceKey));

        result.IsSuccess.Should().BeTrue();

        return new TaxonomySyncRunResponse(
            result.Value.Id,
            result.Value.MarketplaceKey,
            result.Value.Status,
            result.Value.ProcessedCount,
            result.Value.TotalEstimate);
    }

    private static async Task ProcessPendingSyncRunsAsync(WebApplicationFactory<Program> factory)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<IProcessTaxonomySyncHandler>();
            var processed = await handler.ExecuteAsync();
            processed.IsSuccess.Should().BeTrue();

            if (!processed.Value)
            {
                break;
            }
        }
    }

    private sealed record TaxonomySyncRunResponse(
        Guid Id,
        string MarketplaceKey,
        string Status,
        int ProcessedCount,
        int? TotalEstimate);

    private sealed record TaxonomyStatusResponse(
        string MarketplaceKey,
        bool IsSyncActive,
        int CachedCategoryCount);

    private sealed record ExternalCategoryResponse(
        Guid Id,
        string ExternalId,
        string Name,
        string Path,
        bool IsLeaf);
}
