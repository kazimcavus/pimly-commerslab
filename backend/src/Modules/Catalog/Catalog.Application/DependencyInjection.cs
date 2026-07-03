using Catalog.Application.Attributes.AddAttributeValue;
using Catalog.Application.Attributes.CreateAttribute;
using Catalog.Application.Attributes.DeleteAttribute;
using Catalog.Application.Attributes.GetAttribute;
using Catalog.Application.Attributes.ListAttributes;
using Catalog.Application.Attributes.ListAttributeValues;
using Catalog.Application.Attributes.RemoveAttributeValue;
using Catalog.Application.Attributes.UpdateAttribute;
using Catalog.Application.Attributes.UpdateAttributeValue;
using Catalog.Application.Barcodes.AllocateBarcodes;
using Catalog.Application.Barcodes.GetBarcodeSequence;
using Catalog.Application.Barcodes.ListBarcodeAllocations;
using Catalog.Application.Barcodes.UpdateBarcodeSequence;
using Catalog.Application.Categories.AssignCategoryAttribute;
using Catalog.Application.Categories.CreateCategory;
using Catalog.Application.Categories.DeleteCategory;
using Catalog.Application.Categories.GetCategory;
using Catalog.Application.Categories.ListCategories;
using Catalog.Application.Categories.ListCategoryAttributes;
using Catalog.Application.Categories.RemoveCategoryAttribute;
using Catalog.Application.Categories.UpdateCategory;
using Catalog.Application.Categories.UpdateCategoryAttribute;
using Catalog.Application.Products.AddProductImage;
using Catalog.Application.Products.CreateProduct;
using Catalog.Application.Products.CreateProductsBatch;
using Catalog.Application.Products.DeleteItemChannelPrice;
using Catalog.Application.Products.DeleteProduct;
using Catalog.Application.Products.DeleteProductItem;
using Catalog.Application.Products.GetProduct;
using Catalog.Application.Products.GetProductItem;
using Catalog.Application.Products.ListItemChannelPrices;
using Catalog.Application.Products.ListProducts;
using Catalog.Application.Products.RemoveProductImage;
using Catalog.Application.Products.UpdateProduct;
using Catalog.Application.Products.UpdateProductImage;
using Catalog.Application.Products.UpdateProductItem;
using Catalog.Application.Products.UpsertItemChannelPrice;
using Catalog.Application.SkuGenerator;
using Catalog.Application.SkuGenerator.GetSkuGeneratorConfig;
using Catalog.Application.SkuGenerator.UpdateSkuGeneratorConfig;
using Catalog.Application.Variants.AddVariantValue;
using Catalog.Application.Variants.CreateVariantType;
using Catalog.Application.Variants.DeleteVariantType;
using Catalog.Application.Variants.GetVariantType;
using Catalog.Application.Variants.ListVariantTypes;
using Catalog.Application.Variants.ListVariantValues;
using Catalog.Application.Variants.RemoveVariantValue;
using Catalog.Application.Variants.UpdateVariantType;
using Catalog.Application.Variants.UpdateVariantValue;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Application;

/// <summary>Catalog.Application modülü için bağımlılık enjeksiyonu yapılandırması.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddCatalogApplication(this IServiceCollection services)
    {
        services.AddScoped<ICreateCategoryHandler, CreateCategoryHandler>();
        services.AddScoped<IUpdateCategoryHandler, UpdateCategoryHandler>();
        services.AddScoped<IDeleteCategoryHandler, DeleteCategoryHandler>();
        services.AddScoped<IGetCategoryHandler, GetCategoryHandler>();
        services.AddScoped<IListCategoriesHandler, ListCategoriesHandler>();
        services.AddScoped<IAssignCategoryAttributeHandler, AssignCategoryAttributeHandler>();
        services.AddScoped<IListCategoryAttributesHandler, ListCategoryAttributesHandler>();
        services.AddScoped<IUpdateCategoryAttributeHandler, UpdateCategoryAttributeHandler>();
        services.AddScoped<IRemoveCategoryAttributeHandler, RemoveCategoryAttributeHandler>();

        services.AddScoped<ICreateAttributeHandler, CreateAttributeHandler>();
        services.AddScoped<IUpdateAttributeHandler, UpdateAttributeHandler>();
        services.AddScoped<IDeleteAttributeHandler, DeleteAttributeHandler>();
        services.AddScoped<IGetAttributeHandler, GetAttributeHandler>();
        services.AddScoped<IListAttributesHandler, ListAttributesHandler>();
        services.AddScoped<IAddAttributeValueHandler, AddAttributeValueHandler>();
        services.AddScoped<IUpdateAttributeValueHandler, UpdateAttributeValueHandler>();
        services.AddScoped<IRemoveAttributeValueHandler, RemoveAttributeValueHandler>();
        services.AddScoped<IListAttributeValuesHandler, ListAttributeValuesHandler>();

        services.AddScoped<ICreateVariantTypeHandler, CreateVariantTypeHandler>();
        services.AddScoped<IUpdateVariantTypeHandler, UpdateVariantTypeHandler>();
        services.AddScoped<IDeleteVariantTypeHandler, DeleteVariantTypeHandler>();
        services.AddScoped<IGetVariantTypeHandler, GetVariantTypeHandler>();
        services.AddScoped<IListVariantTypesHandler, ListVariantTypesHandler>();
        services.AddScoped<IAddVariantValueHandler, AddVariantValueHandler>();
        services.AddScoped<IUpdateVariantValueHandler, UpdateVariantValueHandler>();
        services.AddScoped<IRemoveVariantValueHandler, RemoveVariantValueHandler>();
        services.AddScoped<IListVariantValuesHandler, ListVariantValuesHandler>();

        services.AddScoped<ICreateProductHandler, CreateProductHandler>();
        services.AddScoped<ICreateProductsBatchHandler, CreateProductsBatchHandler>();
        services.AddScoped<IGetProductHandler, GetProductHandler>();
        services.AddScoped<IListProductsHandler, ListProductsHandler>();
        services.AddScoped<IUpdateProductHandler, UpdateProductHandler>();
        services.AddScoped<IDeleteProductHandler, DeleteProductHandler>();
        services.AddScoped<IAddProductImageHandler, AddProductImageHandler>();
        services.AddScoped<IUpdateProductImageHandler, UpdateProductImageHandler>();
        services.AddScoped<IRemoveProductImageHandler, RemoveProductImageHandler>();
        services.AddScoped<IGetProductItemHandler, GetProductItemHandler>();
        services.AddScoped<IUpdateProductItemHandler, UpdateProductItemHandler>();
        services.AddScoped<IDeleteProductItemHandler, DeleteProductItemHandler>();
        services.AddScoped<IUpsertItemChannelPriceHandler, UpsertItemChannelPriceHandler>();
        services.AddScoped<IListItemChannelPricesHandler, ListItemChannelPricesHandler>();
        services.AddScoped<IDeleteItemChannelPriceHandler, DeleteItemChannelPriceHandler>();

        services.AddScoped<IGetBarcodeSequenceHandler, GetBarcodeSequenceHandler>();
        services.AddScoped<IUpdateBarcodeSequenceHandler, UpdateBarcodeSequenceHandler>();
        services.AddScoped<IAllocateBarcodesHandler, AllocateBarcodesHandler>();
        services.AddScoped<IListBarcodeAllocationsHandler, ListBarcodeAllocationsHandler>();

        services.AddScoped<ISkuGeneratorService, SkuGeneratorService>();
        services.AddScoped<IGetSkuGeneratorConfigHandler, GetSkuGeneratorConfigHandler>();
        services.AddScoped<IUpdateSkuGeneratorConfigHandler, UpdateSkuGeneratorConfigHandler>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
