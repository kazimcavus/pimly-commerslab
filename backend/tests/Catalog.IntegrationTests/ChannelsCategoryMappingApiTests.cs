using System.Net;
using System.Net.Http.Json;
using Catalog.IntegrationTests.Infrastructure;
using Channels.Application.TaxonomySync.EnqueueTaxonomySync;
using Channels.Application.TaxonomySync.ProcessTaxonomySync;
using Channels.Domain.Marketplaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.IntegrationTests;

/// <summary>Channels catalog ↔ marketplace kategori eşlemesi API testleri.</summary>
public class ChannelsCategoryMappingApiTests(CatalogPostgresFixture fixture) : CatalogIntegrationTestBase(fixture)
{
    private const string LeafExternalId = "221";
    private const string NonLeafExternalId = "100";

    [SkippableFact]
    public async Task CategoryChannelMapping_HappyPath()
    {
        await EnsureExternalCategoriesAsync();
        var catalogCategoryId = await CreateCategoryAsync("Gömlek Kategorisi");

        var upsertResponse = await Client.PutAsJsonAsync(
            $"/api/v1/channels/marketplaces/TY/category-mappings/{catalogCategoryId}",
            new { external_id = LeafExternalId });

        upsertResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var mapping = await upsertResponse.Content.ReadFromJsonAsync<CategoryChannelMappingResponse>(CatalogJson.Options);
        mapping!.CatalogCategoryId.Should().Be(catalogCategoryId);
        mapping.ExternalId.Should().Be(LeafExternalId);
        mapping.MarketplaceCode.Should().Be("TY");
        mapping.CatalogCategory!.Name.Should().Be("Gömlek Kategorisi");
        mapping.ExternalCategory!.Name.Should().Be("Gömlek");
        mapping.ExternalCategory.IsLeaf.Should().BeTrue();
    }

    [SkippableFact]
    public async Task CategoryChannelMapping_UpdateExisting_ReplacesExternalId()
    {
        await EnsureExternalCategoriesAsync();
        var catalogCategoryId = await CreateCategoryAsync();

        var firstResponse = await Client.PutAsJsonAsync(
            $"/api/v1/channels/marketplaces/TY/category-mappings/{catalogCategoryId}",
            new { external_id = LeafExternalId });
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondResponse = await Client.PutAsJsonAsync(
            $"/api/v1/channels/marketplaces/TY/category-mappings/{catalogCategoryId}",
            new { external_id = "211" });
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await secondResponse.Content.ReadFromJsonAsync<CategoryChannelMappingResponse>(CatalogJson.Options);
        updated!.ExternalId.Should().Be("211");
        updated.ExternalCategory!.Name.Should().Be("Elbise");
    }

    [SkippableFact]
    public async Task CategoryChannelMapping_WhenCatalogCategoryNotFound_ReturnsNotFound()
    {
        await EnsureExternalCategoriesAsync();
        var missingCategoryId = Guid.NewGuid();

        var response = await Client.PutAsJsonAsync(
            $"/api/v1/channels/marketplaces/TY/category-mappings/{missingCategoryId}",
            new { external_id = LeafExternalId });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [SkippableFact]
    public async Task CategoryChannelMapping_WhenExternalCategoryNotFound_ReturnsNotFound()
    {
        await EnsureExternalCategoriesAsync();
        var catalogCategoryId = await CreateCategoryAsync();

        var response = await Client.PutAsJsonAsync(
            $"/api/v1/channels/marketplaces/TY/category-mappings/{catalogCategoryId}",
            new { external_id = "missing-category" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [SkippableFact]
    public async Task CategoryChannelMapping_WhenExternalCategoryIsNotLeaf_ReturnsBadRequest()
    {
        await EnsureExternalCategoriesAsync();
        var catalogCategoryId = await CreateCategoryAsync();

        var response = await Client.PutAsJsonAsync(
            $"/api/v1/channels/marketplaces/TY/category-mappings/{catalogCategoryId}",
            new { external_id = NonLeafExternalId });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [SkippableFact]
    public async Task CategoryChannelMapping_CrudFlow()
    {
        await EnsureExternalCategoriesAsync();
        var catalogCategoryId = await CreateCategoryAsync("CRUD Category");

        var upsertResponse = await Client.PutAsJsonAsync(
            $"/api/v1/channels/marketplaces/TY/category-mappings/{catalogCategoryId}",
            new { external_id = LeafExternalId });
        upsertResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await Client.GetAsync(
            $"/api/v1/channels/marketplaces/TY/category-mappings/{catalogCategoryId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getMapping = await getResponse.Content.ReadFromJsonAsync<CategoryChannelMappingResponse>(CatalogJson.Options);
        getMapping!.ExternalId.Should().Be(LeafExternalId);

        var listResponse = await Client.GetAsync(
            $"/api/v1/channels/marketplaces/TY/category-mappings?catalog_category_id={catalogCategoryId}");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await listResponse.Content.ReadFromJsonAsync<PagedCategoryChannelMappingsResponse>(CatalogJson.Options);
        list!.Items.Should().ContainSingle(item => item.CatalogCategoryId == catalogCategoryId);

        var deleteResponse = await Client.DeleteAsync(
            $"/api/v1/channels/marketplaces/TY/category-mappings/{catalogCategoryId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getAfterDelete = await Client.GetAsync(
            $"/api/v1/channels/marketplaces/TY/category-mappings/{catalogCategoryId}");
        getAfterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task EnsureExternalCategoriesAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var enqueue = scope.ServiceProvider.GetRequiredService<IEnqueueTaxonomySyncHandler>();
        var marketplace = Marketplace.FromCode("TY").Value;
        var enqueueResult = await enqueue.ExecuteAsync(new EnqueueTaxonomySyncCommand(marketplace));
        enqueueResult.IsSuccess.Should().BeTrue();

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

    private sealed record CategoryChannelMappingResponse(
        Guid Id,
        Guid CatalogCategoryId,
        string MarketplaceCode,
        string ExternalId,
        CatalogCategorySnapshotResponse? CatalogCategory,
        ExternalCategorySummaryResponse? ExternalCategory);

    private sealed record CatalogCategorySnapshotResponse(
        Guid Id,
        string Name,
        string? Code);

    private sealed record ExternalCategorySummaryResponse(
        string ExternalId,
        string Name,
        string Path,
        bool IsLeaf,
        DateTimeOffset SyncedAt);

    private sealed record PagedCategoryChannelMappingsResponse(
        IReadOnlyList<CategoryChannelMappingResponse> Items,
        int Page,
        int PageSize,
        int TotalCount);
}
