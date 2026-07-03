using Channels.Api.Requests;
using Channels.Application.Connections.GetMarketplaceConnection;
using Channels.Application.Connections.UpsertMarketplaceConnection;
using Channels.Application.Marketplaces.ListMarketplaces;
using Channels.Application.Taxonomy.DeleteAttributeChannelMapping;
using Channels.Application.Taxonomy.DeleteCategoryChannelMapping;
using Channels.Application.Taxonomy.GetAttributeChannelMapping;
using Channels.Application.Taxonomy.GetCategoryChannelMapping;
using Channels.Application.Taxonomy.GetTaxonomyStatus;
using Channels.Application.Taxonomy.GetTaxonomySyncRun;
using Channels.Application.Taxonomy.ListAttributeChannelMappings;
using Channels.Application.Taxonomy.ListAttributeValueChannelMappings;
using Channels.Application.Taxonomy.ListCategoryChannelMappings;
using Channels.Application.Taxonomy.ListExternalCategoryAttributes;
using Channels.Application.Taxonomy.SearchExternalCategories;
using Channels.Application.Taxonomy.UpsertAttributeChannelMapping;
using Channels.Application.Taxonomy.UpsertAttributeValueChannelMappings;
using Channels.Application.Taxonomy.UpsertCategoryChannelMapping;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pimly.AspNetCore;

namespace Channels.Api;

/// <summary>Channels modülü REST API uç noktalarını kaydeder.</summary>
public static class ChannelsEndpoints
{
    /// <summary>Channels modülü endpoint'lerini uygulama pipeline'ına kaydeder.</summary>
    public static RouteGroupBuilder MapChannelsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/channels")
            .WithTags("Channels")
            .RequireAuthorization();

        group.MapGet("/marketplaces", async (
            IListMarketplacesHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(new ListMarketplacesQuery(), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapGet("/marketplaces/{key}/connection", async (
            string key,
            IGetMarketplaceConnectionHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(new GetMarketplaceConnectionQuery(key), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapPut("/marketplaces/{key}/connection", async (
            string key,
            UpsertMarketplaceConnectionRequest request,
            IUpsertMarketplaceConnectionHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(
                new UpsertMarketplaceConnectionCommand(
                    key,
                    request.SellerId,
                    request.ApiKey,
                    request.ApiSecret,
                    request.IsEnabled),
                cancellationToken);

            return result.ToHttpResult();
        });

        group.MapGet("/marketplaces/{key}/taxonomy/status", async (
            string key,
            IGetTaxonomyStatusHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(new GetTaxonomyStatusQuery(key), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapGet("/marketplaces/{key}/taxonomy/sync-runs/{syncRunId:guid}", async (
            string key,
            Guid syncRunId,
            IGetTaxonomySyncRunHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(
                new GetTaxonomySyncRunQuery(key, syncRunId),
                cancellationToken);

            return result.ToHttpResult();
        });

        group.MapGet("/marketplaces/{key}/categories", async (
            string key,
            string? q,
            int? limit,
            ISearchExternalCategoriesHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(
                new SearchExternalCategoriesQuery(key, q, limit ?? 25),
                cancellationToken);

            return result.ToHttpResult();
        });

        group.MapPut("/marketplaces/{key}/category-mappings/{catalogCategoryId:guid}", async (
            string key,
            Guid catalogCategoryId,
            UpsertCategoryChannelMappingRequest request,
            IUpsertCategoryChannelMappingHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(
                new UpsertCategoryChannelMappingCommand(key, catalogCategoryId, request.ExternalId),
                cancellationToken);

            return result.ToHttpResult();
        });

        group.MapGet("/marketplaces/{key}/category-mappings/{catalogCategoryId:guid}", async (
            string key,
            Guid catalogCategoryId,
            IGetCategoryChannelMappingHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(
                new GetCategoryChannelMappingQuery(key, catalogCategoryId),
                cancellationToken);

            return result.ToHttpResult();
        });

        group.MapGet("/marketplaces/{key}/category-mappings", async (
            string key,
            Guid? catalog_category_id,
            int? page,
            int? page_size,
            IListCategoryChannelMappingsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(
                new ListCategoryChannelMappingsQuery(
                    key,
                    catalog_category_id,
                    page ?? 0,
                    page_size ?? 0),
                cancellationToken);

            return result.ToHttpResult();
        });

        group.MapDelete("/marketplaces/{key}/category-mappings/{catalogCategoryId:guid}", async (
            string key,
            Guid catalogCategoryId,
            IDeleteCategoryChannelMappingHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(
                new DeleteCategoryChannelMappingCommand(key, catalogCategoryId),
                cancellationToken);

            return result.ToHttpResult();
        });

        group.MapGet("/marketplaces/{key}/category-mappings/{catalogCategoryId:guid}/external-attributes", async (
            string key,
            Guid catalogCategoryId,
            IListExternalCategoryAttributesHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(
                new ListExternalCategoryAttributesQuery(key, catalogCategoryId),
                cancellationToken);

            return result.ToHttpResult();
        });

        group.MapPut("/marketplaces/{key}/category-mappings/{catalogCategoryId:guid}/attribute-mappings", async (
            string key,
            Guid catalogCategoryId,
            UpsertAttributeChannelMappingRequest request,
            IUpsertAttributeChannelMappingHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(
                new UpsertAttributeChannelMappingCommand(
                    key,
                    catalogCategoryId,
                    request.SourceType,
                    request.CatalogSourceId,
                    request.ExternalAttributeId),
                cancellationToken);

            return result.ToHttpResult();
        });

        group.MapGet("/marketplaces/{key}/category-mappings/{catalogCategoryId:guid}/attribute-mappings", async (
            string key,
            Guid catalogCategoryId,
            string? source_type,
            int? page,
            int? page_size,
            IListAttributeChannelMappingsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(
                new ListAttributeChannelMappingsQuery(
                    key,
                    catalogCategoryId,
                    source_type,
                    page ?? 0,
                    page_size ?? 0),
                cancellationToken);

            return result.ToHttpResult();
        });

        group.MapGet("/marketplaces/{key}/category-mappings/{catalogCategoryId:guid}/attribute-mappings/{mappingId:guid}", async (
            string key,
            Guid catalogCategoryId,
            Guid mappingId,
            IGetAttributeChannelMappingHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(
                new GetAttributeChannelMappingQuery(key, catalogCategoryId, mappingId),
                cancellationToken);

            return result.ToHttpResult();
        });

        group.MapDelete("/marketplaces/{key}/category-mappings/{catalogCategoryId:guid}/attribute-mappings/{mappingId:guid}", async (
            string key,
            Guid catalogCategoryId,
            Guid mappingId,
            IDeleteAttributeChannelMappingHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(
                new DeleteAttributeChannelMappingCommand(key, catalogCategoryId, mappingId),
                cancellationToken);

            return result.ToHttpResult();
        });

        group.MapPut("/marketplaces/{key}/category-mappings/{catalogCategoryId:guid}/attribute-mappings/{mappingId:guid}/value-mappings", async (
            string key,
            Guid catalogCategoryId,
            Guid mappingId,
            UpsertAttributeValueChannelMappingsRequest request,
            IUpsertAttributeValueChannelMappingsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(
                new UpsertAttributeValueChannelMappingsCommand(
                    key,
                    catalogCategoryId,
                    mappingId,
                    request.Values
                        .Select(entry => new AttributeValueChannelMappingEntry(
                            entry.CatalogValueId,
                            entry.ExternalValueId))
                        .ToList()),
                cancellationToken);

            return result.ToHttpResult();
        });

        group.MapGet("/marketplaces/{key}/category-mappings/{catalogCategoryId:guid}/attribute-mappings/{mappingId:guid}/value-mappings", async (
            string key,
            Guid catalogCategoryId,
            Guid mappingId,
            IListAttributeValueChannelMappingsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(
                new ListAttributeValueChannelMappingsQuery(key, catalogCategoryId, mappingId),
                cancellationToken);

            return result.ToHttpResult();
        });

        return group;
    }
}
