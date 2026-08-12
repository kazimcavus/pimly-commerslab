using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Catalog.Infrastructure.Persistence;
using Catalog.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Pimly.Outbox;
using Pimly.Outbox.Worker;

namespace Catalog.IntegrationTests;

/// <summary>Outbox yazma tarafı için entegrasyon testleri: ürün oluşturulunca olay kalıcılaşır mı.</summary>
public class OutboxIntegrationTests : CatalogIntegrationTestBase
{
    private readonly string _connectionString;

    public OutboxIntegrationTests(CatalogPostgresFixture fixture)
        : base(fixture)
    {
        _connectionString = fixture.ConnectionString;
    }

    [SkippableFact]
    public async Task CreatingProduct_WritesProductItemCreatedToOutbox()
    {
        var categoryId = await CreateCategoryAsync();

        var createResponse = await Client.PostAsJsonAsync("/api/v1/catalog/products", new
        {
            group_id = Guid.NewGuid(),
            category_id = categoryId,
            model_code = $"OBX-{Guid.NewGuid():N}",
            name = "Outbox Product",
            status = "draft",
            attribute_values = Array.Empty<object>(),
            variants = Array.Empty<object>(),
            items = new[]
            {
                new { barcode = NextNumericBarcode(), price = 10m, stock = 5 },
            },
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        using var document = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var itemId = document.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();

        // Olay, ürün kaydıyla aynı transaction'da outbox'a yazılmış olmalı.
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "select count(*) from catalog.outbox_messages " +
            "where type like '%ProductItemCreated' and payload->>'product_item_id' = @itemId",
            connection);
        command.Parameters.AddWithValue("itemId", itemId.ToString());

        var count = (long)(await command.ExecuteScalarAsync())!;
        count.Should().Be(1);
    }

    [SkippableFact]
    public async Task DeletingProduct_DispatchesProductItemDeleted_RemovesPricingItemPrices()
    {
        var categoryId = await CreateCategoryAsync();

        // 1. Ürün + kalem oluştur (ProductItemCreated outbox'a yazılır).
        var createResponse = await Client.PostAsJsonAsync("/api/v1/catalog/products", new
        {
            group_id = Guid.NewGuid(),
            category_id = categoryId,
            model_code = $"OBX-DEL-{Guid.NewGuid():N}",
            name = "Outbox Delete Product",
            status = "draft",
            attribute_values = Array.Empty<object>(),
            variants = Array.Empty<object>(),
            items = new[]
            {
                new { barcode = NextNumericBarcode(), price = 10m, stock = 5 },
            },
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        using var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var productId = created.RootElement.GetProperty("id").GetGuid();
        var itemId = created.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();

        // 2. Pricing'de fiyat tanımı + kalem fiyatı oluştur.
        var definitionResponse = await Client.PostAsJsonAsync("/api/v1/pricing/price-definitions", new
        {
            name = $"TY Satış {Guid.NewGuid():N}",
            code = (string?)null,
        });
        definitionResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var definition = JsonDocument.Parse(await definitionResponse.Content.ReadAsStringAsync());
        var definitionId = definition.RootElement.GetProperty("id").GetGuid();

        var upsertResponse = await Client.PutAsJsonAsync(
            $"/api/v1/pricing/items/{itemId}/prices/{definitionId}",
            new { amount = 449.90m, currency = (string?)null });
        upsertResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var basePriceResponse = await Client.PutAsJsonAsync(
            $"/api/v1/pricing/items/{itemId}/base-price",
            new { amount = 449.90m, compare_at_amount = 599.90m, currency = (string?)null });
        basePriceResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var channelPriceResponse = await Client.PutAsJsonAsync(
            $"/api/v1/pricing/items/{itemId}/channel-prices/TY",
            new { amount = 429.90m, compare_at_amount = 599.90m, currency = (string?)null });
        channelPriceResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var stockResponse = await Client.PutAsJsonAsync(
            $"/api/v1/inventory/items/{itemId}/stock",
            new { quantity = 7 });
        stockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Kanal fiyatı yazma yolunu doğrula (GET geri okuma).
        var channelPriceRead = await Client.GetFromJsonAsync<ChannelPriceReadResponse>(
            $"/api/v1/pricing/items/{itemId}/channel-prices/TY", CatalogJson.Options);
        channelPriceRead!.Marketplace.Should().Be("TY");
        channelPriceRead.Amount.Should().Be(429.90m);

        (await CountPricingItemPricesAsync(itemId)).Should().Be(1);
        (await CountPricingBasePricesAsync(itemId)).Should().Be(1);
        (await CountPricingChannelPricesAsync(itemId)).Should().Be(1);
        (await CountInventoryStockAsync(itemId)).Should().Be(1);

        // 3. Ürünü sil → her kalem için ProductItemDeleted outbox'a yazılır.
        var deleteResponse = await Client.DeleteAsync($"/api/v1/catalog/products/{productId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 4. Worker kompozisyonuyla outbox'ı işle (tenant her mesaj için mesajdan akar).
        await DispatchOutboxAsync();

        // 5. Pricing fiyatları, temel/kanal fiyatları ve Inventory stoğu temizlenmiş olmalı.
        (await CountPricingItemPricesAsync(itemId)).Should().Be(0);
        (await CountPricingBasePricesAsync(itemId)).Should().Be(0);
        (await CountPricingChannelPricesAsync(itemId)).Should().Be(0);
        (await CountInventoryStockAsync(itemId)).Should().Be(0);
    }

    private sealed record ChannelPriceReadResponse(string Marketplace, decimal Amount);

    private async Task DispatchOutboxAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = _connectionString,
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddPimlyOutboxWorker(configuration);

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor<CatalogDbContext>>();

        // Birkaç tur: bekleyen tüm mesajlar işlenene kadar.
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var processed = await processor.ProcessPendingAsync(batchSize: 50);
            if (processed == 0)
            {
                break;
            }
        }
    }

    private async Task<long> CountPricingItemPricesAsync(Guid itemId) =>
        await CountAsync("select count(*) from pricing.product_item_prices where product_item_id = @itemId", itemId);

    private async Task<long> CountPricingBasePricesAsync(Guid itemId) =>
        await CountAsync("select count(*) from pricing.base_prices where product_item_id = @itemId", itemId);

    private async Task<long> CountPricingChannelPricesAsync(Guid itemId) =>
        await CountAsync("select count(*) from pricing.channel_prices where product_item_id = @itemId", itemId);

    private async Task<long> CountInventoryStockAsync(Guid itemId) =>
        await CountAsync("select count(*) from inventory.stock_levels where product_item_id = @itemId", itemId);

    private async Task<long> CountAsync(string sql, Guid itemId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("itemId", itemId);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private static string NextNumericBarcode() =>
        (8690000000000 + Random.Shared.NextInt64(0, 999_999_999)).ToString(CultureInfo.InvariantCulture);
}
