using Catalog.Application.Contracts;
using Catalog.Application.SkuGenerator;
using Catalog.Application.Validation;
using Catalog.Domain;
using Catalog.Domain.Products;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Products.CreateProduct;

/// <summary>Tek ürün oluşturma işlemini yürüten handler.</summary>
public sealed class CreateProductHandler(
    IValidator<CreateProductCommand> validator,
    IProductRepository products,
    ICategoryRepository categories,
    IVariantRepository variantTypes,
    IAttributeRepository attributes,
    ISkuGeneratorService skuGenerator,
    IUnitOfWork unitOfWork) : ICreateProductHandler
{
    /// <inheritdoc/>
    public async Task<Result<ProductDto>> ExecuteAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<ProductDto>(validationResult.Error);
        }

        var categoryExists = await categories.GetByIdAsync(command.CategoryId, cancellationToken);
        if (categoryExists is null)
        {
            return Result.Failure<ProductDto>(Error.NotFound("Category not found."));
        }

        var resolvedTypesResult = await ProductCreationSupport.ResolveVariantsAsync(
            variantTypes,
            command.Variants,
            cancellationToken);

        if (resolvedTypesResult.IsFailure)
        {
            return Result.Failure<ProductDto>(resolvedTypesResult.Error);
        }

        if (resolvedTypesResult.Value.Any(type => type.Slicer))
        {
            return Result.Failure<ProductDto>(
                Error.Validation("Products with a slicer variant type must be created using POST /products:batch."));
        }

        var attributeValuesResult = await ProductCreationSupport.ResolveAttributeValuesAsync(
            attributes,
            command.AttributeValueInputs,
            cancellationToken);

        if (attributeValuesResult.IsFailure)
        {
            return Result.Failure<ProductDto>(attributeValuesResult.Error);
        }

        var itemDraftsResult = await ProductCreationSupport.ResolveItemDraftsAsync(
            variantTypes,
            attributes,
            command.Items,
            cancellationToken);

        if (itemDraftsResult.IsFailure)
        {
            return Result.Failure<ProductDto>(itemDraftsResult.Error);
        }

        var plansResult = await skuGenerator.BuildPlansAsync(
            command.ModelCode,
            command.CodeInputs,
            command.Name,
            resolvedTypesResult.Value,
            itemDraftsResult.Value,
            cancellationToken);

        if (plansResult.IsFailure)
        {
            return Result.Failure<ProductDto>(plansResult.Error);
        }

        var plan = plansResult.Value.Single();

        var persistenceUniquenessResult = await ProductCreationSupport.EnsurePlanIsUniqueAsync(
            products,
            plan,
            cancellationToken);

        if (persistenceUniquenessResult.IsFailure)
        {
            return Result.Failure<ProductDto>(persistenceUniquenessResult.Error);
        }

        var status = ProductMappings.ParseStatus(command.Status);
        var createResult = Product.Create(
            command.GroupId,
            command.CategoryId,
            plan.ModelCode,
            plan.Name,
            status,
            attributeValuesResult.Value,
            plan.Variants,
            plan.Items.ToList());

        if (createResult.IsFailure)
        {
            return Result.Failure<ProductDto>(createResult.Error);
        }

        await products.AddAsync(createResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(createResult.Value.ToDto());
    }
}
