using System.Net;
using System.Net.Http.Json;
using Catalog.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.IntegrationTests;

/// <summary>Ürün import API uç noktaları için entegrasyon testleri (kuyruk davranışı hariç).</summary>
public class ChannelsProductImportApiTests(CatalogPostgresFixture fixture) : CatalogIntegrationTestBase(fixture)
{
    [SkippableFact]
    public async Task StartImport_WithoutConnection_ReturnsNotFound()
    {
        var response = await Client.PostAsync("/api/v1/channels/marketplaces/TY/imports", null);
        await CatalogHttpAssertions.AssertProblemAsync(response, HttpStatusCode.NotFound, "not_found");
    }

    [SkippableFact]
    public async Task StartImport_ConnectionWithoutSellerId_ReturnsValidationError()
    {
        await UpsertConnectionAsync(sellerId: null);

        var response = await Client.PostAsync("/api/v1/channels/marketplaces/TY/imports", null);
        await CatalogHttpAssertions.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");
    }

    [SkippableFact]
    public async Task StartImport_HappyPath_ReturnsAcceptedRun_AndDuplicateConflicts()
    {
        await UpsertConnectionAsync(sellerId: "seller-1");

        var response = await Client.PostAsync("/api/v1/channels/marketplaces/TY/imports", null);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var run = await response.Content.ReadFromJsonAsync<ProductImportRunResponse>(CatalogJson.Options);
        run.Should().NotBeNull();
        run!.MarketplaceCode.Should().Be("TY");
        run.Status.Should().Be("pending");

        // Aynı tenant + pazaryeri için ikinci istek 409 dönmeli.
        var duplicate = await Client.PostAsync("/api/v1/channels/marketplaces/TY/imports", null);
        await CatalogHttpAssertions.AssertProblemAsync(duplicate, HttpStatusCode.Conflict, "conflict");

        var getResponse = await Client.GetAsync($"/api/v1/channels/marketplaces/TY/imports/{run.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<ProductImportRunResponse>(CatalogJson.Options);
        fetched!.Id.Should().Be(run.Id);

        var listResponse = await Client.GetAsync("/api/v1/channels/marketplaces/TY/imports");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var runs = await listResponse.Content.ReadFromJsonAsync<List<ProductImportRunSummaryResponse>>(CatalogJson.Options);
        runs!.Should().ContainSingle(r => r.Id == run.Id);
    }

    [SkippableFact]
    public async Task GetImportRun_OtherTenantsRun_ReturnsNotFound()
    {
        await UpsertConnectionAsync(sellerId: "seller-2");
        var response = await Client.PostAsync("/api/v1/channels/marketplaces/TY/imports", null);
        var run = await response.Content.ReadFromJsonAsync<ProductImportRunResponse>(CatalogJson.Options);

        var otherClient = IntegrationTestAuth.CreateAuthenticatedClient(
            Factory,
            $"import-other-{Guid.NewGuid():N}@example.com",
            "other-password-123",
            "Other Tenant",
            $"Other Tenant {Guid.NewGuid():N}");

        var foreignGet = await otherClient.GetAsync($"/api/v1/channels/marketplaces/TY/imports/{run!.Id}");
        await CatalogHttpAssertions.AssertProblemAsync(foreignGet, HttpStatusCode.NotFound, "not_found");
    }

    [SkippableFact]
    public async Task EnqueueTaxonomySync_ViaHttp_ReturnsAccepted()
    {
        var response = await Client.PostAsync("/api/v1/channels/marketplaces/TY/taxonomy/sync-runs", null);

        // Aynı pazaryeri için sync zaten kuyruktaysa (başka testten) 409 kabul edilir.
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Accepted, HttpStatusCode.Conflict);

        // Taksonomi kuyruğu pazaryeri-globaldir; bekleyen run bırakmak diğer testleri
        // (enqueue → conflict) etkiler. Kuyruğu boşaltarak bitir.
        await using var scope = Factory.Services.CreateAsyncScope();
        var process = scope.ServiceProvider.GetRequiredService<Channels.Application.TaxonomySync.ProcessTaxonomySync.IProcessTaxonomySyncHandler>();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var processed = await process.ExecuteAsync();
            if (processed.IsFailure || !processed.Value)
            {
                break;
            }
        }
    }

    private async Task UpsertConnectionAsync(string? sellerId)
    {
        var response = await Client.PutAsJsonAsync("/api/v1/channels/marketplaces/TY/connection", new
        {
            seller_id = sellerId,
            api_key = "import-test-api-key",
            api_secret = "import-test-secret",
            is_enabled = true,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record ProductImportRunResponse(
        Guid Id,
        string MarketplaceCode,
        string Status,
        int? TotalProducts,
        int ProcessedProducts,
        int ImportedProducts,
        int SkippedProducts,
        int FailedProducts,
        string? ErrorMessage);

    private sealed record ProductImportRunSummaryResponse(
        Guid Id,
        string MarketplaceCode,
        string Status);
}
