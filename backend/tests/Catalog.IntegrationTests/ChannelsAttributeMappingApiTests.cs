using System.Net;
using System.Net.Http.Json;
using Catalog.IntegrationTests.Infrastructure;
using Channels.Application.TaxonomySync.EnqueueTaxonomySync;
using Channels.Application.TaxonomySync.ProcessTaxonomySync;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel;

namespace Catalog.IntegrationTests;

/// <summary>Channels catalog attribute/variant ↔ marketplace eşlemesi API testleri.</summary>
public class ChannelsAttributeMappingApiTests(CatalogPostgresFixture fixture) : CatalogIntegrationTestBase(fixture)
{
    private const string GomlekExternalId = "221";
    private const string PhoneExternalId = "111";
    private const string MarketplaceRouteCode = "TY";

    [SkippableFact]
    public async Task ExternalAttributesSync_ReturnsStubData()
    {
        await EnsureExternalCategoriesAsync();
        var catalogCategoryId = await CreateMappedCategoryAsync(GomlekExternalId);

        var response = await Client.GetAsync(
            $"/api/v1/channels/marketplaces/{MarketplaceRouteCode}/category-mappings/{catalogCategoryId}/external-attributes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var attributes = await response.Content.ReadFromJsonAsync<List<ExternalCategoryAttributeResponse>>(CatalogJson.Options);
        attributes.Should().NotBeNull();
        attributes!.Should().Contain(item => item.ExternalAttributeId == "attr-beden");
        attributes.Should().Contain(item => item.ExternalAttributeId == "attr-kumas");
        attributes.Single(item => item.ExternalAttributeId == "attr-beden").Values.Should().Contain(
            value => value.ExternalValueId == "val-s");
    }

    [SkippableFact]
    public async Task AttributeFieldMapping_HappyPath()
    {
        await EnsureExternalCategoriesAsync();
        var catalogCategoryId = await CreateMappedCategoryAsync(GomlekExternalId);
        await SyncExternalAttributesAsync(catalogCategoryId);

        var attribute_id = await CreateAttributeWithValueAsync("Kumaş Tipi", "Pamuk");
        await AssignAttributeToCategoryAsync(catalogCategoryId, attribute_id);

        var response = await Client.PutAsJsonAsync(
            $"/api/v1/channels/marketplaces/{MarketplaceRouteCode}/category-mappings/{catalogCategoryId}/attribute-mappings",
            new
            {
                source_type = "catalog_attribute",
                catalog_source_id = attribute_id,
                external_attribute_id = "attr-kumas",
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var mapping = await response.Content.ReadFromJsonAsync<AttributeChannelMappingResponse>(CatalogJson.Options);
        mapping!.SourceType.Should().Be("catalog_attribute");
        mapping.CatalogSourceId.Should().Be(attribute_id);
        mapping.ExternalAttributeId.Should().Be("attr-kumas");
        mapping.ExternalAttribute!.Name.Should().Be("Kumaş");
    }

    [SkippableFact]
    public async Task VariantFieldMapping_HappyPath()
    {
        await EnsureExternalCategoriesAsync();
        var catalogCategoryId = await CreateMappedCategoryAsync(GomlekExternalId);
        await SyncExternalAttributesAsync(catalogCategoryId);

        var variantId = await CreateVariantWithValueAsync("Beden", "S");

        var response = await Client.PutAsJsonAsync(
            $"/api/v1/channels/marketplaces/{MarketplaceRouteCode}/category-mappings/{catalogCategoryId}/attribute-mappings",
            new
            {
                source_type = "catalog_variant",
                catalog_source_id = variantId.VariantId,
                external_attribute_id = "attr-beden",
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var mapping = await response.Content.ReadFromJsonAsync<AttributeChannelMappingResponse>(CatalogJson.Options);
        mapping!.SourceType.Should().Be("catalog_variant");
        mapping.CatalogVariant!.Id.Should().Be(variantId.VariantId);
        mapping.ExternalAttribute!.IsVariant.Should().BeTrue();
    }

    [SkippableFact]
    public async Task ExternalAttributes_WhenCategoryMappingMissing_ReturnsNotFound()
    {
        await EnsureExternalCategoriesAsync();
        var catalogCategoryId = await CreateCategoryAsync();

        var response = await Client.GetAsync(
            $"/api/v1/channels/marketplaces/{MarketplaceRouteCode}/category-mappings/{catalogCategoryId}/external-attributes");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [SkippableFact]
    public async Task AttributeFieldMapping_WhenAttributeNotAssignedToCategory_ReturnsNotFound()
    {
        await EnsureExternalCategoriesAsync();
        var catalogCategoryId = await CreateMappedCategoryAsync(GomlekExternalId);
        await SyncExternalAttributesAsync(catalogCategoryId);

        var attribute_id = await CreateAttributeWithValueAsync("Atanmamış Özellik", "Değer");

        var response = await Client.PutAsJsonAsync(
            $"/api/v1/channels/marketplaces/{MarketplaceRouteCode}/category-mappings/{catalogCategoryId}/attribute-mappings",
            new
            {
                source_type = "catalog_attribute",
                catalog_source_id = attribute_id,
                external_attribute_id = "attr-kumas",
            });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [SkippableFact]
    public async Task AttributeFieldMapping_WhenExternalAttributeNotFound_ReturnsNotFound()
    {
        await EnsureExternalCategoriesAsync();
        var catalogCategoryId = await CreateMappedCategoryAsync(GomlekExternalId);
        await SyncExternalAttributesAsync(catalogCategoryId);

        var attribute_id = await CreateAttributeWithValueAsync("Renk", "Mavi");
        await AssignAttributeToCategoryAsync(catalogCategoryId, attribute_id);

        var response = await Client.PutAsJsonAsync(
            $"/api/v1/channels/marketplaces/{MarketplaceRouteCode}/category-mappings/{catalogCategoryId}/attribute-mappings",
            new
            {
                source_type = "catalog_attribute",
                catalog_source_id = attribute_id,
                external_attribute_id = "missing-attribute",
            });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [SkippableFact]
    public async Task ValueMappingBatchUpsert_HappyPath()
    {
        await EnsureExternalCategoriesAsync();
        var catalogCategoryId = await CreateMappedCategoryAsync(GomlekExternalId);
        await SyncExternalAttributesAsync(catalogCategoryId);

        var attribute_id = await CreateAttributeWithValueAsync("Kumaş", "Pamuk");
        var valueId = await GetAttributeValueIdAsync(attribute_id, "Pamuk");
        await AssignAttributeToCategoryAsync(catalogCategoryId, attribute_id);

        var fieldMapping = await UpsertFieldMappingAsync(
            catalogCategoryId,
            "catalog_attribute",
            attribute_id,
            "attr-kumas");

        var response = await Client.PutAsJsonAsync(
            $"/api/v1/channels/marketplaces/{MarketplaceRouteCode}/category-mappings/{catalogCategoryId}/attribute-mappings/{fieldMapping.Id}/value-mappings",
            new
            {
                values = new[]
                {
                    new { catalog_value_id = valueId, external_value_id = "val-pamuk" },
                },
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var valueMappings = await response.Content.ReadFromJsonAsync<List<AttributeValueChannelMappingResponse>>(CatalogJson.Options);
        valueMappings!.Should().ContainSingle(item =>
            item.CatalogValueId == valueId && item.ExternalValueId == "val-pamuk");

        var listResponse = await Client.GetAsync(
            $"/api/v1/channels/marketplaces/{MarketplaceRouteCode}/category-mappings/{catalogCategoryId}/attribute-mappings/{fieldMapping.Id}/value-mappings");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listed = await listResponse.Content.ReadFromJsonAsync<List<AttributeValueChannelMappingResponse>>(CatalogJson.Options);
        listed!.Should().ContainSingle(item => item.ExternalValueId == "val-pamuk");
    }

    [SkippableFact]
    public async Task ValueMapping_AllowCustomAttribute_DoesNotRequireCachedExternalValue()
    {
        await EnsureExternalCategoriesAsync();
        var catalogCategoryId = await CreateMappedCategoryAsync(PhoneExternalId);
        await SyncExternalAttributesAsync(catalogCategoryId);

        var attribute_id = await CreateAttributeWithValueAsync("Marka", "Özel Marka");
        var valueId = await GetAttributeValueIdAsync(attribute_id, "Özel Marka");
        await AssignAttributeToCategoryAsync(catalogCategoryId, attribute_id);

        var fieldMapping = await UpsertFieldMappingAsync(
            catalogCategoryId,
            "catalog_attribute",
            attribute_id,
            "attr-marka");

        var response = await Client.PutAsJsonAsync(
            $"/api/v1/channels/marketplaces/{MarketplaceRouteCode}/category-mappings/{catalogCategoryId}/attribute-mappings/{fieldMapping.Id}/value-mappings",
            new
            {
                values = new[]
                {
                    new { catalog_value_id = valueId, external_value_id = "custom-brand-123" },
                },
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task DeleteFieldMapping_CascadesValueMappings()
    {
        await EnsureExternalCategoriesAsync();
        var catalogCategoryId = await CreateMappedCategoryAsync(GomlekExternalId);
        await SyncExternalAttributesAsync(catalogCategoryId);

        var attribute_id = await CreateAttributeWithValueAsync("Kumaş", "Pamuk");
        var valueId = await GetAttributeValueIdAsync(attribute_id, "Pamuk");
        await AssignAttributeToCategoryAsync(catalogCategoryId, attribute_id);

        var fieldMapping = await UpsertFieldMappingAsync(
            catalogCategoryId,
            "catalog_attribute",
            attribute_id,
            "attr-kumas");

        var upsertValuesResponse = await Client.PutAsJsonAsync(
            $"/api/v1/channels/marketplaces/{MarketplaceRouteCode}/category-mappings/{catalogCategoryId}/attribute-mappings/{fieldMapping.Id}/value-mappings",
            new
            {
                values = new[]
                {
                    new { catalog_value_id = valueId, external_value_id = "val-pamuk" },
                },
            });
        upsertValuesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteResponse = await Client.DeleteAsync(
            $"/api/v1/channels/marketplaces/{MarketplaceRouteCode}/category-mappings/{catalogCategoryId}/attribute-mappings/{fieldMapping.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getFieldResponse = await Client.GetAsync(
            $"/api/v1/channels/marketplaces/{MarketplaceRouteCode}/category-mappings/{catalogCategoryId}/attribute-mappings/{fieldMapping.Id}");
        getFieldResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [SkippableFact]
    public async Task AttributeMapping_CrudFlow()
    {
        await EnsureExternalCategoriesAsync();
        var catalogCategoryId = await CreateMappedCategoryAsync(GomlekExternalId);
        await SyncExternalAttributesAsync(catalogCategoryId);

        var attribute_id = await CreateAttributeWithValueAsync("Kumaş", "Pamuk");
        var valueId = await GetAttributeValueIdAsync(attribute_id, "Pamuk");
        await AssignAttributeToCategoryAsync(catalogCategoryId, attribute_id);

        var fieldMapping = await UpsertFieldMappingAsync(
            catalogCategoryId,
            "catalog_attribute",
            attribute_id,
            "attr-kumas");

        var getResponse = await Client.GetAsync(
            $"/api/v1/channels/marketplaces/{MarketplaceRouteCode}/category-mappings/{catalogCategoryId}/attribute-mappings/{fieldMapping.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listResponse = await Client.GetAsync(
            $"/api/v1/channels/marketplaces/{MarketplaceRouteCode}/category-mappings/{catalogCategoryId}/attribute-mappings?source_type=catalog_attribute");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await listResponse.Content.ReadFromJsonAsync<PagedAttributeChannelMappingsResponse>(CatalogJson.Options);
        list!.Items.Should().ContainSingle(item => item.Id == fieldMapping.Id);

        var valueResponse = await Client.PutAsJsonAsync(
            $"/api/v1/channels/marketplaces/{MarketplaceRouteCode}/category-mappings/{catalogCategoryId}/attribute-mappings/{fieldMapping.Id}/value-mappings",
            new
            {
                values = new[]
                {
                    new { catalog_value_id = valueId, external_value_id = "val-pamuk" },
                },
            });
        valueResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteResponse = await Client.DeleteAsync(
            $"/api/v1/channels/marketplaces/{MarketplaceRouteCode}/category-mappings/{catalogCategoryId}/attribute-mappings/{fieldMapping.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task<Guid> CreateMappedCategoryAsync(string externalCategoryId)
    {
        var catalogCategoryId = await CreateCategoryAsync($"AttrMap-{Guid.NewGuid():N}");

        var response = await Client.PutAsJsonAsync(
            $"/api/v1/channels/marketplaces/{MarketplaceRouteCode}/category-mappings/{catalogCategoryId}",
            new { external_id = externalCategoryId });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return catalogCategoryId;
    }

    private async Task SyncExternalAttributesAsync(Guid catalogCategoryId)
    {
        var response = await Client.GetAsync(
            $"/api/v1/channels/marketplaces/{MarketplaceRouteCode}/category-mappings/{catalogCategoryId}/external-attributes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<Guid> CreateAttributeWithValueAsync(string attributeName, string valueName)
    {
        var createResponse = await Client.PostAsJsonAsync("/api/v1/catalog/attributes", new { name = attributeName });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var attribute = await createResponse.Content.ReadFromJsonAsync<AttributeResponse>(CatalogJson.Options);

        var valueResponse = await Client.PostAsJsonAsync(
            $"/api/v1/catalog/attributes/{attribute!.Id}/values",
            new { name = valueName });
        valueResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        return attribute.Id;
    }

    private async Task<Guid> GetAttributeValueIdAsync(Guid attribute_id, string valueName)
    {
        var listResponse = await Client.GetAsync($"/api/v1/catalog/attributes/{attribute_id}/values");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var values = await listResponse.Content.ReadFromJsonAsync<PagedAttributeValuesResponse>(CatalogJson.Options);
        return values!.Items.Single(item => item.Name == valueName).Id;
    }

    private async Task AssignAttributeToCategoryAsync(Guid categoryId, Guid attribute_id)
    {
        var response = await Client.PostAsJsonAsync(
            $"/api/v1/catalog/categories/{categoryId}/attributes",
            new { attribute_id = attribute_id, required = true, sort_order = 0 });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private async Task<(Guid VariantId, Guid ValueId)> CreateVariantWithValueAsync(string variantName, string valueLabel)
    {
        var createVariantResponse = await Client.PostAsJsonAsync("/api/v1/catalog/variants", new
        {
            name = $"{variantName}-{Guid.NewGuid():N}",
            selection_style = "list",
            sort_order = 0,
            slicer = true,
        });
        createVariantResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var variant = await createVariantResponse.Content.ReadFromJsonAsync<VariantResponse>(CatalogJson.Options);

        var createValueResponse = await Client.PostAsJsonAsync(
            $"/api/v1/catalog/variants/{variant!.Id}/values",
            new { label = valueLabel, sort_order = 0 });
        createValueResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var value = await createValueResponse.Content.ReadFromJsonAsync<VariantValueResponse>(CatalogJson.Options);

        return (variant.Id, value!.Id);
    }

    private async Task<AttributeChannelMappingResponse> UpsertFieldMappingAsync(
        Guid catalogCategoryId,
        string sourceType,
        Guid catalogSourceId,
        string externalAttributeId)
    {
        var response = await Client.PutAsJsonAsync(
            $"/api/v1/channels/marketplaces/{MarketplaceRouteCode}/category-mappings/{catalogCategoryId}/attribute-mappings",
            new
            {
                source_type = sourceType,
                catalog_source_id = catalogSourceId,
                external_attribute_id = externalAttributeId,
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var mapping = await response.Content.ReadFromJsonAsync<AttributeChannelMappingResponse>(CatalogJson.Options);
        return mapping!;
    }

    private async Task EnsureExternalCategoriesAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var enqueue = scope.ServiceProvider.GetRequiredService<IEnqueueTaxonomySyncHandler>();
        var marketplace = Marketplace.FromCode(MarketplaceRouteCode).Value;
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

    private sealed record AttributeResponse(Guid Id, string Key, string Name);

    private sealed record AttributeValueResponse(Guid Id, string Name);

    private sealed record PagedAttributeValuesResponse(
        IReadOnlyList<AttributeValueResponse> Items,
        int Page,
        int PageSize,
        int TotalCount);

    private sealed record VariantResponse(Guid Id, string Key, string Name);

    private sealed record VariantValueResponse(Guid Id, string Label);

    private sealed record ExternalCategoryAttributeResponse(
        string ExternalCategoryId,
        string ExternalAttributeId,
        string Name,
        bool Required,
        bool AllowCustom,
        bool IsVariant,
        DateTimeOffset SyncedAt,
        IReadOnlyList<ExternalAttributeValueResponse> Values);

    private sealed record ExternalAttributeValueResponse(
        string ExternalAttributeId,
        string ExternalValueId,
        string Name,
        DateTimeOffset SyncedAt);

    private sealed record AttributeChannelMappingResponse(
        Guid Id,
        Guid CatalogCategoryId,
        string MarketplaceCode,
        string SourceType,
        Guid CatalogSourceId,
        string ExternalAttributeId,
        CatalogAttributeSnapshotResponse? CatalogAttribute,
        CatalogVariantSnapshotResponse? CatalogVariant,
        ExternalCategoryAttributeSummaryResponse? ExternalAttribute);

    private sealed record CatalogAttributeSnapshotResponse(Guid Id, string Key, string Name);

    private sealed record CatalogVariantSnapshotResponse(Guid Id, string Key, string Name);

    private sealed record ExternalCategoryAttributeSummaryResponse(
        string ExternalAttributeId,
        string Name,
        bool Required,
        bool AllowCustom,
        bool IsVariant);

    private sealed record AttributeValueChannelMappingResponse(
        Guid Id,
        Guid AttributeChannelMappingId,
        Guid CatalogValueId,
        string ExternalValueId,
        string? CatalogValueName,
        ExternalAttributeValueSummaryResponse? ExternalValue);

    private sealed record ExternalAttributeValueSummaryResponse(string ExternalValueId, string Name);

    private sealed record PagedAttributeChannelMappingsResponse(
        IReadOnlyList<AttributeChannelMappingResponse> Items,
        int Page,
        int PageSize,
        int TotalCount);
}
