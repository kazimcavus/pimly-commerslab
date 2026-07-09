using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Pricing.Application.BasePrices.GetBasePrice;
using Pricing.Application.BasePrices.SetBasePrice;
using Pricing.Application.ChannelPrices.GetChannelPrice;
using Pricing.Application.ChannelPrices.ListChannelPrices;
using Pricing.Application.ChannelPrices.SetChannelPrice;
using Pricing.Application.ItemPrices.DeleteItemPrice;
using Pricing.Application.ItemPrices.DeleteItemPricesForItem;
using Pricing.Application.ItemPrices.ListItemPrices;
using Pricing.Application.ItemPrices.UpsertItemPrice;
using Pricing.Application.PriceDefinitions.CreatePriceDefinition;
using Pricing.Application.PriceDefinitions.DeletePriceDefinition;
using Pricing.Application.PriceDefinitions.GetPriceDefinition;
using Pricing.Application.PriceDefinitions.ListPriceDefinitions;
using Pricing.Application.PriceDefinitions.UpdatePriceDefinition;

namespace Pricing.Application;

/// <summary>Pricing.Application modülü için bağımlılık enjeksiyonu yapılandırması.</summary>
public static class DependencyInjection
{
    /// <summary>Pricing uygulama handler'larını ve validator'larını kaydeder.</summary>
    public static IServiceCollection AddPricingApplication(this IServiceCollection services)
    {
        services.AddScoped<ICreatePriceDefinitionHandler, CreatePriceDefinitionHandler>();
        services.AddScoped<IUpdatePriceDefinitionHandler, UpdatePriceDefinitionHandler>();
        services.AddScoped<IDeletePriceDefinitionHandler, DeletePriceDefinitionHandler>();
        services.AddScoped<IGetPriceDefinitionHandler, GetPriceDefinitionHandler>();
        services.AddScoped<IListPriceDefinitionsHandler, ListPriceDefinitionsHandler>();

        services.AddScoped<IUpsertItemPriceHandler, UpsertItemPriceHandler>();
        services.AddScoped<IListItemPricesHandler, ListItemPricesHandler>();
        services.AddScoped<IDeleteItemPriceHandler, DeleteItemPriceHandler>();
        services.AddScoped<IDeleteItemPricesForItemHandler, DeleteItemPricesForItemHandler>();

        services.AddScoped<ISetBasePriceHandler, SetBasePriceHandler>();
        services.AddScoped<IGetBasePriceHandler, GetBasePriceHandler>();

        services.AddScoped<ISetChannelPriceHandler, SetChannelPriceHandler>();
        services.AddScoped<IGetChannelPriceHandler, GetChannelPriceHandler>();
        services.AddScoped<IListChannelPricesHandler, ListChannelPricesHandler>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
