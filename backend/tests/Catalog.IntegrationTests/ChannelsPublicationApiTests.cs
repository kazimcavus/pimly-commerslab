using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Catalog.IntegrationTests.Infrastructure;
using Channels.Application.Publications.ProcessPublication;
using Channels.Domain.Publications;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pimly.ProductPublications.Worker;
using SharedKernel.Tenancy;

namespace Catalog.IntegrationTests;

/// <summary>Ürün yayın (publication) enqueue + işleme + durum sorgu akışı için entegrasyon testleri.</summary>
public class ChannelsPublicationApiTests(CatalogPostgresFixture fixture) : CatalogIntegrationTestBase(fixture)
{
    private readonly CatalogPostgresFixture _fixture = fixture;

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

    [SkippableFact]
    public async Task Publication_EndToEnd_PublishesChannelPricedItems()
    {
        await UpsertTrendyolConnectionAsync();

        // Kalem oluştur ve TY kanal fiyatı ata (yayın kaynağı).
        var itemId = await CreateProductItemAsync();
        var channelPrice = await Client.PutAsJsonAsync(
            $"/api/v1/pricing/items/{itemId}/channel-prices/TY",
            new { amount = 449.90m, compare_at_amount = 599.90m, currency = (string?)null });
        channelPrice.StatusCode.Should().Be(HttpStatusCode.OK);

        // Yayını kuyruğa al.
        var enqueue = await Client.PostAsync("/api/v1/channels/marketplaces/TY/publications", null);
        enqueue.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var run = (await enqueue.Content.ReadFromJsonAsync<PublicationRunResponse>(CatalogJson.Options))!;

        // Worker kompozisyonuyla işle (stub listing client).
        await ProcessQueuedPublicationsAsync();

        var finished = await Client.GetFromJsonAsync<PublicationRunResponse>(
            $"/api/v1/channels/marketplaces/TY/publications/{run.Id}", CatalogJson.Options);
        finished!.Status.Should().Be("completed");
        finished.TotalItems.Should().Be(1);
        finished.PublishedItems.Should().Be(1);
        finished.FailedItems.Should().Be(0);
    }

    private async Task<Guid> CreateProductItemAsync()
    {
        var categoryId = await CreateCategoryAsync();
        var createResponse = await Client.PostAsJsonAsync("/api/v1/catalog/products", new
        {
            group_id = Guid.NewGuid(),
            category_id = categoryId,
            model_code = $"PUB-{Guid.NewGuid():N}",
            name = "Publication Product",
            status = "draft",
            attribute_values = Array.Empty<object>(),
            variants = Array.Empty<object>(),
            items = new[] { new { barcode = NextNumericBarcode(), stock = 5 } },
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        using var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        return created.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();
    }

    private async Task ProcessQueuedPublicationsAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = _fixture.ConnectionString,
                ["Channels:UseStubTaxonomyClient"] = "true",
                ["ProductImports:PollIntervalSeconds"] = "1",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddPimlyProductPublicationsWorker(configuration);

        await using var provider = services.BuildServiceProvider();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            Guid runId;
            Guid tenantId;

            await using (var claimScope = provider.CreateAsyncScope())
            {
                var publicationRuns = claimScope.ServiceProvider.GetRequiredService<IProductPublicationRunRepository>();
                var claimed = await publicationRuns.TryClaimNextPendingAsync();
                if (claimed is null)
                {
                    return;
                }

                runId = claimed.Id;
                tenantId = claimed.TenantId;
            }

            await using var processScope = provider.CreateAsyncScope();
            processScope.ServiceProvider.GetRequiredService<AmbientTenantContext>().Set(tenantId);
            var handler = processScope.ServiceProvider.GetRequiredService<IProcessPublicationHandler>();
            var result = await handler.ExecuteAsync(runId);
            result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : string.Empty);
        }
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

    private static string NextNumericBarcode() =>
        (8690000000000 + Random.Shared.NextInt64(0, 999_999_999)).ToString(CultureInfo.InvariantCulture);

    private sealed record PublicationRunResponse(
        Guid Id,
        string MarketplaceCode,
        string Status,
        int? TotalItems,
        int ProcessedItems,
        int PublishedItems,
        int FailedItems);
}
