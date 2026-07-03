using Channels.Application.Connections.GetMarketplaceConnection;
using Channels.Application.Connections.UpsertMarketplaceConnection;
using Channels.Application.Marketplaces.ListMarketplaces;
using Channels.Application.Taxonomy.DeleteAttributeChannelMapping;
using Channels.Application.Taxonomy.DeleteAttributeValueChannelMapping;
using Channels.Application.Taxonomy.DeleteCategoryChannelMapping;
using Channels.Application.Taxonomy.EnqueueTaxonomySync;
using Channels.Application.Taxonomy.GetAttributeChannelMapping;
using Channels.Application.Taxonomy.GetCategoryChannelMapping;
using Channels.Application.Taxonomy.GetTaxonomyStatus;
using Channels.Application.Taxonomy.GetTaxonomySyncRun;
using Channels.Application.Taxonomy.ListAttributeChannelMappings;
using Channels.Application.Taxonomy.ListAttributeValueChannelMappings;
using Channels.Application.Taxonomy.ListCategoryChannelMappings;
using Channels.Application.Taxonomy.ListExternalCategoryAttributes;
using Channels.Application.Taxonomy.ProcessTaxonomySync;
using Channels.Application.Taxonomy.ResolveAttributeChannelMapping;
using Channels.Application.Taxonomy.ResolveAttributeValueChannelMapping;
using Channels.Application.Taxonomy.ResolveCategoryChannelMapping;
using Channels.Application.Taxonomy.RunScheduledTaxonomySync;
using Channels.Application.Taxonomy.SearchExternalCategories;
using Channels.Application.Taxonomy.UpsertAttributeChannelMapping;
using Channels.Application.Taxonomy.UpsertAttributeValueChannelMappings;
using Channels.Application.Taxonomy.UpsertCategoryChannelMapping;
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

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
