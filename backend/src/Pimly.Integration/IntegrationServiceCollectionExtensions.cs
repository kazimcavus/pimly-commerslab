using Channels.Application.AttributeChannelMappings.Catalog;
using Channels.Application.CategoryChannelMappings.Catalog;
using Channels.Application.Listings.ContentSync;
using Channels.Application.Listings.OfferSync;
using Channels.Application.ProductImports.Catalog;
using Channels.Application.Publications;
using Microsoft.Extensions.DependencyInjection;

namespace Pimly.Integration;

/// <summary>
/// Modüller arası ACL gateway adaptörlerini DI'a kaydeder. Adaptörler bir modülün portunu başka bir
/// modülün use-case'ine bağlar; bu yüzden modüllerin kendisinde değil, kompozisyon katmanında yaşarlar.
/// </summary>
/// <remarks>
/// Her host yalnızca ihtiyaç duyduğu grubu kaydeder — böylece worker'ın hangi modüllere gerçekten
/// dokunduğu kompozisyondan okunabilir kalır.
/// </remarks>
public static class IntegrationServiceCollectionExtensions
{
    /// <summary>Channels'ın Catalog'dan kategori/attribute/varyant okumasını sağlayan gateway'ler.</summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <returns>Zincirleme için aynı servis koleksiyonu.</returns>
    public static IServiceCollection AddCatalogReadGateways(this IServiceCollection services)
    {
        services.AddScoped<ICatalogCategoryGateway, CatalogCategoryGateway>();
        services.AddScoped<ICatalogAttributeGateway, CatalogAttributeGateway>();
        services.AddScoped<ICatalogVariantGateway, CatalogVariantGateway>();
        return services;
    }

    /// <summary>
    /// Pricing ve Inventory'nin, fiyat/stok yazmadan önce kalemin varlığını doğrulamasını sağlayan
    /// gateway'ler. İki modül aynı arabirim adını kullandığı için kayıtlar tam nitelikli verilir.
    /// </summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <returns>Zincirleme için aynı servis koleksiyonu.</returns>
    public static IServiceCollection AddProductItemExistenceGateways(this IServiceCollection services)
    {
        services.AddScoped<Pricing.Application.ItemPrices.Catalog.ICatalogProductItemGateway,
            PricingCatalogProductItemGateway>();
        services.AddScoped<Inventory.Application.StockLevels.Catalog.ICatalogProductItemGateway,
            InventoryCatalogProductItemGateway>();
        return services;
    }

    /// <summary>Ürün import hattının Catalog'a yazma kapısı (yalnız import worker'ı için).</summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <returns>Zincirleme için aynı servis koleksiyonu.</returns>
    public static IServiceCollection AddCatalogImportGateway(this IServiceCollection services)
    {
        services.AddScoped<ICatalogImportGateway, CatalogImportGateway>();
        return services;
    }

    /// <summary>Channels'ın Pricing'den kararlaştırılmış kanal fiyatlarını okumasını sağlayan gateway.</summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <returns>Zincirleme için aynı servis koleksiyonu.</returns>
    public static IServiceCollection AddPricingChannelPriceGateway(this IServiceCollection services)
    {
        services.AddScoped<IPricingChannelPriceGateway, PricingChannelPriceGateway>();
        return services;
    }

    /// <summary>Channels senkronunun Inventory'den stok okumasını sağlayan gateway.</summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <returns>Zincirleme için aynı servis koleksiyonu.</returns>
    public static IServiceCollection AddInventoryStockGateway(this IServiceCollection services)
    {
        services.AddScoped<IInventoryStockGateway, InventoryStockGateway>();
        return services;
    }

    /// <summary>Channels'ın Catalog'dan pazaryerine gidecek ürün içeriğini okumasını sağlayan gateway.</summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <returns>Zincirleme için aynı servis koleksiyonu.</returns>
    public static IServiceCollection AddCatalogListingSourceGateway(this IServiceCollection services)
    {
        services.AddScoped<ICatalogListingSourceGateway, CatalogListingSourceGateway>();
        return services;
    }
}
