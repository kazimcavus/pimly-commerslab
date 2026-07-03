using System.Net;
using System.Net.Http.Json;
using Catalog.IntegrationTests.Infrastructure;
using Channels.Application.Imports.ProcessProductImport;
using Channels.Application.TaxonomySync.EnqueueTaxonomySync;
using Channels.Application.TaxonomySync.ProcessTaxonomySync;
using Channels.Domain.Imports;
using Channels.Domain.Marketplaces;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pimly.Channels.Worker;
using SharedKernel.Tenancy;

namespace Catalog.IntegrationTests;

/// <summary>
/// Trendyol import hattının uçtan uca testi: API üzerinden bağlantı + import kuyruğu,
/// worker kompozisyonuyla işleme, HTTP üzerinden katalog/eşleme/fiyat doğrulamaları.
/// Stub client'lar kullanılır (Gömlek/Telefon sahte kataloğu).
/// </summary>
public class ProductImportEndToEndTests(CatalogPostgresFixture fixture) : CatalogIntegrationTestBase(fixture)
{
    private readonly CatalogPostgresFixture _fixture = fixture;

    [SkippableFact]
    public async Task TrendyolImport_EndToEnd_BuildsCatalogMappingsAndChannelPrices()
    {
        // --- 1. Bağlantı + taksonomi cache ---
        await UpsertConnectionAsync();
        await EnsureTaxonomySyncedAsync();

        // --- 2. Import'u kuyruğa al (HTTP) ---
        var startResponse = await Client.PostAsync("/api/v1/channels/marketplaces/TY/imports", null);
        startResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var run = (await startResponse.Content.ReadFromJsonAsync<ImportRunResponse>(CatalogJson.Options))!;

        // --- 3. Worker kompozisyonuyla işle ---
        await ProcessQueuedImportsAsync();

        var finishedRun = await GetRunAsync(run.Id);
        finishedRun.Status.Should().Be("completed", finishedRun.ErrorMessage ?? "run failed");
        finishedRun.TotalProducts.Should().Be(3); // GOMLEK-001, GOMLEK-002, TELEFON-001
        finishedRun.ImportedProducts.Should().Be(3);
        finishedRun.SkippedProducts.Should().Be(0);
        finishedRun.FailedProducts.Should().Be(0);

        // --- 4. Katalog doğrulamaları (HTTP) ---
        // Renk slicer: GOMLEK-001 → 2 ürün (Mavi/Beyaz), GOMLEK-002 → 1, TELEFON-001 → 2.
        var products = await ListAsync<ProductResponse>("/api/v1/catalog/products?page=1&page_size=100");
        products.Should().HaveCount(5);
        products.Sum(p => p.Items.Count).Should().Be(8);

        // Splitter bölünen ürün adına slicer değerini ekler (ör. "Klasik Yaka Gömlek - Mavi").
        products.Should().OnlyContain(p =>
            p.Name.StartsWith("Klasik Yaka Gömlek")
            || p.Name.StartsWith("Oxford Gömlek")
            || p.Name.StartsWith("Pimly Akıllı Telefon 128 GB"));
        products.SelectMany(p => p.Items).Select(i => i.Barcode).Should().OnlyHaveUniqueItems();

        // Kategori zinciri: Moda > Erkek > Gömlek ve Elektronik > Telefon > Akıllı Telefon.
        var categories = await ListAsync<CategoryResponse>("/api/v1/catalog/categories?page=1&page_size=100");
        categories.Select(c => c.Name).Should().Contain(
            ["Moda", "Erkek", "Gömlek", "Elektronik", "Telefon", "Akıllı Telefon"]);
        var gomlek = categories.Single(c => c.Name == "Gömlek");
        categories.Single(c => c.Id == gomlek.ParentId!.Value).Name.Should().Be("Erkek");

        // Varyantlar: Renk (renk stili + slicer) ve Beden; Renk değerleri iki kategoriden birleşir.
        var variants = await ListAsync<VariantTypeResponse>("/api/v1/catalog/variants?page=1&page_size=100");
        var renk = variants.Single(v => v.Name == "Renk");
        renk.SelectionStyle.Should().Be("color");
        renk.Slicer.Should().BeTrue();
        variants.Should().ContainSingle(v => v.Name == "Beden");
        var renkValues = await ListAsync<VariantValueResponse>($"/api/v1/catalog/variants/{renk.Id}/values");
        renkValues.Select(v => v.Label).Should().Contain(["Mavi", "Beyaz", "Siyah"]);

        // Özellikler: Kumaş, Hafıza, Marka özellik olarak (varyant değil) tanımlanır.
        var attributes = await ListAsync<AttributeResponse>("/api/v1/catalog/attributes?page=1&page_size=100");
        attributes.Select(a => a.Name).Should().Contain(["Kumaş", "Hafıza", "Marka"]);
        var gomlekAttrs = await ListAsync<CategoryAttributeResponse>($"/api/v1/catalog/categories/{gomlek.Id}/attributes");
        gomlekAttrs.Select(a => a.Name).Should().Contain("Kumaş");

        // --- 5. Kanal fiyatı: satış 449.90, karşılaştırma 599.90 (ty anahtarıyla) ---
        var gomlekItem = products.SelectMany(p => p.Items).Single(i => i.Barcode == "8680000000011");
        gomlekItem.Price.Should().Be(449.90m);
        gomlekItem.CompareAtPrice.Should().Be(599.90m);
        gomlekItem.Stock.Should().Be(25);

        var channelPrices = await ListAsync<ChannelPriceResponse>($"/api/v1/catalog/items/{gomlekItem.Id}/channel-prices");
        channelPrices.Should().ContainSingle(p => p.MarketplaceKey == "ty" && p.Price == 449.90m);

        // --- 6. Kanal eşlemeleri gönderim fazına hazır ---
        var categoryMappings = await ListAsync<CategoryMappingResponse>(
            "/api/v1/channels/marketplaces/TY/category-mappings?page=1&page_size=100");
        categoryMappings.Select(m => m.ExternalId).Should().Contain(["221", "111"]);
        categoryMappings.Should().Contain(m => m.CatalogCategoryId == gomlek.Id && m.ExternalId == "221");

        var attributeMappings = await ListAsync<AttributeMappingResponse>(
            $"/api/v1/channels/marketplaces/TY/category-mappings/{gomlek.Id}/attribute-mappings?page=1&page_size=100");
        attributeMappings.Should().Contain(m => m.SourceType == "catalog_variant" && m.ExternalAttributeId == "attr-renk-gomlek");
        attributeMappings.Should().Contain(m => m.SourceType == "catalog_variant" && m.ExternalAttributeId == "attr-beden");
        attributeMappings.Should().Contain(m => m.SourceType == "catalog_attribute" && m.ExternalAttributeId == "attr-kumas");

        // --- 7. Tekrar import → tümü atlanır (idempotentlik) ---
        var rerunResponse = await Client.PostAsync("/api/v1/channels/marketplaces/TY/imports", null);
        rerunResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var rerun = (await rerunResponse.Content.ReadFromJsonAsync<ImportRunResponse>(CatalogJson.Options))!;
        await ProcessQueuedImportsAsync();

        var rerunFinished = await GetRunAsync(rerun.Id);
        rerunFinished.Status.Should().Be("completed");
        rerunFinished.SkippedProducts.Should().Be(3);
        rerunFinished.ImportedProducts.Should().Be(0);

        (await ListAsync<ProductResponse>("/api/v1/catalog/products?page=1&page_size=100")).Should().HaveCount(5);

        // --- 8. Başka tenant hiçbir şey görmez ---
        var otherClient = IntegrationTestAuth.CreateAuthenticatedClient(
            Factory,
            $"e2e-other-{Guid.NewGuid():N}@example.com",
            "other-password-123",
            "E2E Other",
            $"E2E Other {Guid.NewGuid():N}");
        var otherProducts = await otherClient.GetFromJsonAsync<PagedResponse<ProductResponse>>(
            "/api/v1/catalog/products?page=1&page_size=100", CatalogJson.Options);
        otherProducts!.Items.Should().BeEmpty();
    }

    private async Task UpsertConnectionAsync()
    {
        var response = await Client.PutAsJsonAsync("/api/v1/channels/marketplaces/TY/connection", new
        {
            seller_id = "seller-e2e",
            api_key = "e2e-api-key",
            api_secret = "e2e-api-secret",
            is_enabled = true,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // Taksonomi cache'ini stub client ile doldurur; eşzamanlı sync varsa (conflict) mevcut kuyruğu işler.
    private async Task EnsureTaxonomySyncedAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var enqueue = scope.ServiceProvider.GetRequiredService<IEnqueueTaxonomySyncHandler>();
        var marketplace = Marketplace.FromCode("TY").Value;
        await enqueue.ExecuteAsync(new EnqueueTaxonomySyncCommand(marketplace)); // conflict tolere edilir

        var process = scope.ServiceProvider.GetRequiredService<IProcessTaxonomySyncHandler>();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var processed = await process.ExecuteAsync();
            processed.IsSuccess.Should().BeTrue();
            if (!processed.Value)
            {
                break;
            }
        }
    }

    // Worker kompozisyonunun aynısını test DB'sine karşı kurar ve kuyruğu boşaltır:
    // scope A tenant'sız claim eder, scope B ambient tenant'ı set edip işler.
    private async Task ProcessQueuedImportsAsync()
    {
        var mediaPath = Path.Combine(Path.GetTempPath(), "pimly-e2e-media", Guid.NewGuid().ToString("N"));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = _fixture.ConnectionString,
                ["Channels:UseStubTaxonomyClient"] = "true",
                ["Channels:WorkerPollIntervalSeconds"] = "1",
                ["Media:StoragePath"] = mediaPath,
                ["Media:AllowedUrlPrefix"] = "/media/",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddPimlyWorker(configuration);

        await using var provider = services.BuildServiceProvider();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            Guid runId;
            Guid tenantId;

            await using (var claimScope = provider.CreateAsyncScope())
            {
                var importRuns = claimScope.ServiceProvider.GetRequiredService<IProductImportRunRepository>();
                var claimed = await importRuns.TryClaimNextPendingAsync();
                if (claimed is null)
                {
                    return;
                }

                runId = claimed.Id;
                tenantId = claimed.TenantId;
            }

            await using var processScope = provider.CreateAsyncScope();
            processScope.ServiceProvider.GetRequiredService<AmbientTenantContext>().Set(tenantId);
            var handler = processScope.ServiceProvider.GetRequiredService<IProcessProductImportHandler>();
            var result = await handler.ExecuteAsync(runId);
            result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : string.Empty);
        }
    }

    private async Task<ImportRunResponse> GetRunAsync(Guid runId)
    {
        var response = await Client.GetAsync($"/api/v1/channels/marketplaces/TY/imports/{runId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ImportRunResponse>(CatalogJson.Options))!;
    }

    // Hem sayfalı zarf ({items: ...}) hem düz dizi dönen uçları tek yardımcıyla okur.
    private async Task<List<T>> ListAsync<T>(string path)
    {
        var response = await Client.GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var document = System.Text.Json.JsonDocument.Parse(json);
        var payload = document.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array
            ? document.RootElement
            : document.RootElement.GetProperty("items");

        return System.Text.Json.JsonSerializer.Deserialize<List<T>>(payload.GetRawText(), CatalogJson.Options) ?? [];
    }

    private sealed record PagedResponse<T>(List<T>? Items);

    private sealed record ImportRunResponse(
        Guid Id,
        string MarketplaceCode,
        string Status,
        int? TotalProducts,
        int ProcessedProducts,
        int ImportedProducts,
        int SkippedProducts,
        int FailedProducts,
        string? ErrorMessage);

    private sealed record ProductResponse(
        Guid Id,
        Guid CategoryId,
        string ModelCode,
        string Name,
        string Status,
        List<ProductItemResponse> Items);

    private sealed record ProductItemResponse(
        Guid Id,
        string? Sku,
        string Barcode,
        decimal Price,
        decimal? CompareAtPrice,
        int Stock);

    private sealed record VariantTypeResponse(Guid Id, string Key, string Name, string SelectionStyle, bool Slicer);

    private sealed record VariantValueResponse(Guid Id, string Label);

    private sealed record AttributeResponse(Guid Id, string Key, string Name);

    private sealed record CategoryAttributeResponse(Guid AttributeId, string Name, bool Required);

    private sealed record ChannelPriceResponse(Guid Id, string MarketplaceKey, decimal Price, decimal? CompareAtPrice);

    private sealed record CategoryMappingResponse(Guid Id, Guid CatalogCategoryId, string ExternalId);

    private sealed record AttributeMappingResponse(
        Guid Id,
        string SourceType,
        Guid CatalogSourceId,
        string ExternalAttributeId);
}
