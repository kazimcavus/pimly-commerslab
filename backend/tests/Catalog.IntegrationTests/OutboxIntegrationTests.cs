using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Catalog.IntegrationTests.Infrastructure;
using FluentAssertions;
using Npgsql;

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

    private static string NextNumericBarcode() =>
        (8690000000000 + Random.Shared.NextInt64(0, 999_999_999)).ToString(CultureInfo.InvariantCulture);
}
