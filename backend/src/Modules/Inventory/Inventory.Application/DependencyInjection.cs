using FluentValidation;
using Inventory.Application.StockLevels.DeleteStockForItem;
using Inventory.Application.StockLevels.GetStock;
using Inventory.Application.StockLevels.SetStock;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Application;

/// <summary>Inventory.Application modülü için bağımlılık enjeksiyonu yapılandırması.</summary>
public static class DependencyInjection
{
    /// <summary>Inventory uygulama handler'larını ve validator'larını kaydeder.</summary>
    public static IServiceCollection AddInventoryApplication(this IServiceCollection services)
    {
        services.AddScoped<ISetStockHandler, SetStockHandler>();
        services.AddScoped<IGetStockHandler, GetStockHandler>();
        services.AddScoped<IDeleteStockForItemHandler, DeleteStockForItemHandler>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
