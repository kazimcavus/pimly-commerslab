using Channels.Application.AttributeChannelMappings.DeleteAttributeChannelMapping;
using Channels.Application.AttributeChannelMappings.DeleteAttributeValueChannelMapping;
using Channels.Application.AttributeChannelMappings.GetAttributeChannelMapping;
using Channels.Application.AttributeChannelMappings.ListAttributeChannelMappings;
using Channels.Application.AttributeChannelMappings.ListAttributeValueChannelMappings;
using Channels.Application.AttributeChannelMappings.ResolveAttributeChannelMapping;
using Channels.Application.AttributeChannelMappings.ResolveAttributeValueChannelMapping;
using Channels.Application.AttributeChannelMappings.UpsertAttributeChannelMapping;
using Channels.Application.AttributeChannelMappings.UpsertAttributeValueChannelMappings;
using Channels.Application.CategoryChannelMappings.DeleteCategoryChannelMapping;
using Channels.Application.CategoryChannelMappings.GetCategoryChannelMapping;
using Channels.Application.CategoryChannelMappings.ListCategoryChannelMappings;
using Channels.Application.CategoryChannelMappings.ResolveCategoryChannelMapping;
using Channels.Application.CategoryChannelMappings.UpsertCategoryChannelMapping;
using Channels.Application.Connections.GetMarketplaceConnection;
using Channels.Application.Connections.UpsertMarketplaceConnection;
using Channels.Application.ExternalCatalog.ListExternalCategoryAttributes;
using Channels.Application.ExternalCatalog.SearchExternalCategories;
using Channels.Application.Marketplaces.ListMarketplaces;
using Channels.Application.ProductImports.EnqueueProductImport;
using Channels.Application.ProductImports.GetProductImportRun;
using Channels.Application.ProductImports.ListProductImportRuns;
using Channels.Application.Publications.EnqueuePublication;
using Channels.Application.Publications.GetPublicationRun;
using Channels.Application.TaxonomySync.EnqueueTaxonomySync;
using Channels.Application.TaxonomySync.GetTaxonomyStatus;
using Channels.Application.TaxonomySync.GetTaxonomySyncRun;
using Channels.Application.TaxonomySync.ProcessTaxonomySync;
using Channels.Application.TaxonomySync.RunScheduledTaxonomySync;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Channels.Application;

/// <summary>Channels.Application modülü için bağımlılık enjeksiyonu yapılandırması.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddChannelsApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IListMarketplacesHandler, ListMarketplacesHandler>();
        services.AddScoped<IGetMarketplaceConnectionHandler, GetMarketplaceConnectionHandler>();
        services.AddScoped<IUpsertMarketplaceConnectionHandler, UpsertMarketplaceConnectionHandler>();
        services.AddScoped<IEnqueueTaxonomySyncHandler, EnqueueTaxonomySyncHandler>();
        services.AddScoped<IRunScheduledTaxonomySyncHandler, RunScheduledTaxonomySyncHandler>();
        services.AddScoped<IGetTaxonomySyncRunHandler, GetTaxonomySyncRunHandler>();
        services.AddScoped<IGetTaxonomyStatusHandler, GetTaxonomyStatusHandler>();
        services.AddScoped<ISearchExternalCategoriesHandler, SearchExternalCategoriesHandler>();
        services.AddScoped<IProcessTaxonomySyncHandler, ProcessTaxonomySyncHandler>();
        services.AddScoped<IUpsertCategoryChannelMappingHandler, UpsertCategoryChannelMappingHandler>();
        services.AddScoped<IGetCategoryChannelMappingHandler, GetCategoryChannelMappingHandler>();
        services.AddScoped<IListCategoryChannelMappingsHandler, ListCategoryChannelMappingsHandler>();
        services.AddScoped<IDeleteCategoryChannelMappingHandler, DeleteCategoryChannelMappingHandler>();
        services.AddScoped<IResolveCategoryChannelMappingHandler, ResolveCategoryChannelMappingHandler>();
        services.AddScoped<IListExternalCategoryAttributesHandler, ListExternalCategoryAttributesHandler>();
        services.AddScoped<IUpsertAttributeChannelMappingHandler, UpsertAttributeChannelMappingHandler>();
        services.AddScoped<IGetAttributeChannelMappingHandler, GetAttributeChannelMappingHandler>();
        services.AddScoped<IListAttributeChannelMappingsHandler, ListAttributeChannelMappingsHandler>();
        services.AddScoped<IDeleteAttributeChannelMappingHandler, DeleteAttributeChannelMappingHandler>();
        services.AddScoped<IResolveAttributeChannelMappingHandler, ResolveAttributeChannelMappingHandler>();
        services.AddScoped<IUpsertAttributeValueChannelMappingsHandler, UpsertAttributeValueChannelMappingsHandler>();
        services.AddScoped<IListAttributeValueChannelMappingsHandler, ListAttributeValueChannelMappingsHandler>();
        services.AddScoped<IDeleteAttributeValueChannelMappingHandler, DeleteAttributeValueChannelMappingHandler>();
        services.AddScoped<IResolveAttributeValueChannelMappingHandler, ResolveAttributeValueChannelMappingHandler>();
        services.AddScoped<IEnqueueProductImportHandler, EnqueueProductImportHandler>();
        services.AddScoped<IGetProductImportRunHandler, GetProductImportRunHandler>();
        services.AddScoped<IEnqueuePublicationHandler, EnqueuePublicationHandler>();
        services.AddScoped<IGetPublicationRunHandler, GetPublicationRunHandler>();
        services.AddScoped<IListProductImportRunsHandler, ListProductImportRunsHandler>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
