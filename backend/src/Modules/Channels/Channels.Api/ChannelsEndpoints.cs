using Channels.Api.Requests;
using Channels.Application.AttributeChannelMappings.DeleteAttributeChannelMapping;
using Channels.Application.AttributeChannelMappings.GetAttributeChannelMapping;
using Channels.Application.AttributeChannelMappings.ListAttributeChannelMappings;
using Channels.Application.AttributeChannelMappings.ListAttributeValueChannelMappings;
using Channels.Application.AttributeChannelMappings.UpsertAttributeChannelMapping;
using Channels.Application.AttributeChannelMappings.UpsertAttributeValueChannelMappings;
using Channels.Application.CategoryChannelMappings.DeleteCategoryChannelMapping;
using Channels.Application.CategoryChannelMappings.GetCategoryChannelMapping;
using Channels.Application.CategoryChannelMappings.ListCategoryChannelMappings;
using Channels.Application.CategoryChannelMappings.UpsertCategoryChannelMapping;
using Channels.Application.Connections.GetMarketplaceConnection;
using Channels.Application.Connections.UpsertMarketplaceConnection;
using Channels.Application.ExternalCatalog.ListExternalCategoryAttributes;
using Channels.Application.ExternalCatalog.SearchExternalCategories;
using Channels.Application.Marketplaces.ListMarketplaces;
using Channels.Application.TaxonomySync.GetTaxonomyStatus;
using Channels.Application.TaxonomySync.GetTaxonomySyncRun;
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

        group.MapGet("/marketplaces/{code}/connection", async (
            string code,
            IGetMarketplaceConnectionHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(new GetMarketplaceConnectionQuery(code), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapPut("/marketplaces/{code}/connection", async (
            string code,
            UpsertMarketplaceConnectionRequest request,
            IUpsertMarketplaceConnectionHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(
                new UpsertMarketplaceConnectionCommand(
                    code,
                    request.SellerId,
                    request.ApiKey,
                    request.ApiSecret,
                    request.IsEnabled),
                cancellationToken);

            return result.ToHttpResult();
        });

        group.MapGet("/marketplaces/{code}/taxonomy/status", async (
            string code,
            IGetTaxonomyStatusHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(new GetTaxonomyStatusQuery(code), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapGet("/marketplaces/{code}/taxonomy/sync-runs/{syncRunId:guid}", async (
            string code,
            Guid syncRunId,
            IGetTaxonomySyncRunHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(
                new GetTaxonomySyncRunQuery(code, syncRunId),
                cancellationToken);

            return result.ToHttpResult();
        });

        group.MapGet("/marketplaces/{code}/categories", async (
            string code,
            string? q,
            int? limit,
            ISearchExternalCategoriesHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(
                new SearchExternalCategoriesQuery(code, q, limit ?? 25),
                cancellationToken);

            return result.ToHttpResult();
        });

        group.MapPut("/marketplaces/{code}/category-mappings/{catalogCategoryId:guid}", async (
            string code,
            Guid catalogCategoryId,
            UpsertCategoryChannelMappingRequest request,
            IUpsertCategoryChannelMappingHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(
                new UpsertCategoryChannelMappingCommand(code, catalogCategoryId, request.ExternalId),
                cancellationToken);

            return result.ToHttpResult();
        });

        group.MapGet("/marketplaces/{code}/category-mappings/{catalogCategoryId:guid}", async (
            string code,
            Guid catalogCategoryId,
            IGetCategoryChannelMappingHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(
                new GetCategoryChannelMappingQuery(code, catalogCategoryId),
                cancellationToken);

            return result.ToHttpResult();
        });

        group.MapGet("/marketplaces/{code}/category-mappings", async (
            string code,
            Guid? catalog_category_id,
            int? page,
            int? page_size,
            IListCategoryChannelMappingsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(
                new ListCategoryChannelMappingsQuery(
                    code,
                    catalog_category_id,
                    page ?? 0,
                    page_size ?? 0),
                cancellationToken);

            return result.ToHttpResult();
        });

        group.MapDelete("/marketplaces/{code}/category-mappings/{catalogCategoryId:guid}", async (
            string code,
            Guid catalogCategoryId,
            IDeleteCategoryChannelMappingHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(
                new DeleteCategoryChannelMappingCommand(code, catalogCategoryId),
                cancellationToken);

            return result.ToHttpResult();
        });

        group.MapGet("/marketplaces/{code}/category-mappings/{catalogCategoryId:guid}/external-attributes", async (
            string code,
            Guid catalogCategoryId,
            IListExternalCategoryAttributesHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(
                new ListExternalCategoryAttributesQuery(code, catalogCategoryId),
                cancellationToken);

            return result.ToHttpResult();
        });

        group.MapPut("/marketplaces/{code}/category-mappings/{catalogCategoryId:guid}/attribute-mappings", async (
            string code,
            Guid catalogCategoryId,
            UpsertAttributeChannelMappingRequest request,
            IUpsertAttributeChannelMappingHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(
                new UpsertAttributeChannelMappingCommand(
                    code,
                    catalogCategoryId,
                    request.SourceType,
                    request.CatalogSourceId,
                    request.ExternalAttributeId),
                cancellationToken);

            return result.ToHttpResult();
        });

        group.MapGet("/marketplaces/{code}/category-mappings/{catalogCategoryId:guid}/attribute-mappings", async (
            string code,
            Guid catalogCategoryId,
            string? source_type,
            int? page,
            int? page_size,
            IListAttributeChannelMappingsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(
                new ListAttributeChannelMappingsQuery(
                    code,
                    catalogCategoryId,
                    source_type,
                    page ?? 0,
                    page_size ?? 0),
                cancellationToken);

            return result.ToHttpResult();
        });

        group.MapGet("/marketplaces/{code}/category-mappings/{catalogCategoryId:guid}/attribute-mappings/{mappingId:guid}", async (
            string code,
            Guid catalogCategoryId,
            Guid mappingId,
            IGetAttributeChannelMappingHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(
                new GetAttributeChannelMappingQuery(code, catalogCategoryId, mappingId),
                cancellationToken);

            return result.ToHttpResult();
        });

        group.MapDelete("/marketplaces/{code}/category-mappings/{catalogCategoryId:guid}/attribute-mappings/{mappingId:guid}", async (
            string code,
            Guid catalogCategoryId,
            Guid mappingId,
            IDeleteAttributeChannelMappingHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(
                new DeleteAttributeChannelMappingCommand(code, catalogCategoryId, mappingId),
                cancellationToken);

            return result.ToHttpResult();
        });

        group.MapPut("/marketplaces/{code}/category-mappings/{catalogCategoryId:guid}/attribute-mappings/{mappingId:guid}/value-mappings", async (
            string code,
            Guid catalogCategoryId,
            Guid mappingId,
            UpsertAttributeValueChannelMappingsRequest request,
            IUpsertAttributeValueChannelMappingsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(
                new UpsertAttributeValueChannelMappingsCommand(
                    code,
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

        group.MapGet("/marketplaces/{code}/category-mappings/{catalogCategoryId:guid}/attribute-mappings/{mappingId:guid}/value-mappings", async (
            string code,
            Guid catalogCategoryId,
            Guid mappingId,
            IListAttributeValueChannelMappingsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.ExecuteAsync(
                new ListAttributeValueChannelMappingsQuery(code, catalogCategoryId, mappingId),
                cancellationToken);

            return result.ToHttpResult();
        });

        return group;
    }
}
