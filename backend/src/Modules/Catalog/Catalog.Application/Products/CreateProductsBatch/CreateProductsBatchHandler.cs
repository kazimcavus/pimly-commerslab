using Catalog.Application.Contracts;
using Catalog.Application.SkuGenerator;
using Catalog.Application.Validation;
using Catalog.Domain;
using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Products.CreateProductsBatch;

/// <summary>Toplu ürün oluşturma işlemini yürüten handler.</summary>
public sealed class CreateProductsBatchHandler(
    IValidator<CreateProductsBatchCommand> validator,
    IProductRepository products,
    ICategoryRepository categories,
    IVariantRepository variantTypes,
    IAttributeRepository attributes,
    ISkuGeneratorService skuGenerator,
    IUnitOfWork unitOfWork) : ICreateProductsBatchHandler
{
    /// <inheritdoc/>
    public async Task<Result<CreateProductsBatchResult>> ExecuteAsync(
        CreateProductsBatchCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<CreateProductsBatchResult>(validationResult.Error);
        }

        var planEntries = new List<PlanEntry>();
        var seenModelCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenBarcodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenVariantSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var categoriesById = new Dictionary<Guid, Category?>();

        foreach (var item in command.Products)
        {
            if (!categoriesById.TryGetValue(item.CategoryId, out var category))
            {
                category = await categories.GetByIdAsync(item.CategoryId, cancellationToken);
                categoriesById[item.CategoryId] = category;
            }

            if (category is null)
            {
                return Result.Failure<CreateProductsBatchResult>(Error.NotFound("Category not found."));
            }

            var requiredAttributesResult = await ProductCreationSupport.EnsureRequiredCategoryAttributesAsync(
                attributes,
                category,
                (item.AttributeValueInputs ?? []).Select(input => input.AttributeId).ToHashSet(),
                cancellationToken);

            if (requiredAttributesResult.IsFailure)
            {
                return Result.Failure<CreateProductsBatchResult>(requiredAttributesResult.Error);
            }

            var resolvedTypesResult = await ProductCreationSupport.ResolveVariantsAsync(
                variantTypes,
                item.Variants,
                cancellationToken);

            if (resolvedTypesResult.IsFailure)
            {
                return Result.Failure<CreateProductsBatchResult>(resolvedTypesResult.Error);
            }

            var attributeValuesResult = await ProductCreationSupport.ResolveAttributeValuesAsync(
                attributes,
                item.AttributeValueInputs,
                cancellationToken);

            if (attributeValuesResult.IsFailure)
            {
                return Result.Failure<CreateProductsBatchResult>(attributeValuesResult.Error);
            }

            var itemDraftsResult = await ProductCreationSupport.ResolveItemDraftsAsync(
                variantTypes,
                attributes,
                item.Items,
                cancellationToken);

            if (itemDraftsResult.IsFailure)
            {
                return Result.Failure<CreateProductsBatchResult>(itemDraftsResult.Error);
            }

            var plansResult = await skuGenerator.BuildPlansAsync(
                item.ModelCode,
                item.CodeInputs,
                item.Name,
                resolvedTypesResult.Value,
                itemDraftsResult.Value,
                item.SplitOverrides,
                cancellationToken);

            if (plansResult.IsFailure)
            {
                return Result.Failure<CreateProductsBatchResult>(plansResult.Error);
            }

            foreach (var plan in plansResult.Value)
            {
                var batchUniquenessResult = EnsureBatchUniqueness(
                    plan,
                    seenModelCodes,
                    seenBarcodes,
                    seenVariantSkus);

                if (batchUniquenessResult.IsFailure)
                {
                    return Result.Failure<CreateProductsBatchResult>(batchUniquenessResult.Error);
                }

                var persistenceUniquenessResult = await ProductCreationSupport.EnsurePlanIsUniqueAsync(
                    products,
                    plan,
                    cancellationToken);

                if (persistenceUniquenessResult.IsFailure)
                {
                    return Result.Failure<CreateProductsBatchResult>(persistenceUniquenessResult.Error);
                }

                planEntries.Add(new PlanEntry(
                    plan,
                    item.CategoryId,
                    ProductMappings.ParseStatus(item.Status),
                    attributeValuesResult.Value,
                    item.BrandId,
                    plan.Description ?? item.Description)); // slicer (renk) açıklaması yoksa grup açıklaması
            }
        }

        var createdProducts = new List<Product>();
        foreach (var entry in planEntries)
        {
            var createResult = Product.Create(
                command.GroupId,
                entry.CategoryId,
                entry.Plan.ModelCode,
                entry.Plan.Name,
                entry.Status,
                entry.AttributeValues,
                entry.Plan.Variants,
                entry.Plan.Items.ToList(),
                entry.Plan.GroupCode,
                entry.Plan.SlicerValue,
                entry.BrandId,
                entry.Description);

            if (createResult.IsFailure)
            {
                return Result.Failure<CreateProductsBatchResult>(createResult.Error);
            }

            createdProducts.Add(createResult.Value);
        }

        foreach (var product in createdProducts)
        {
            await products.AddAsync(product, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateProductsBatchResult(
            createdProducts.Select(product => product.ToDto()).ToList()));
    }

    private static Result EnsureBatchUniqueness(
        ProductCreatePlan plan,
        HashSet<string> seenModelCodes,
        HashSet<string> seenBarcodes,
        HashSet<string> seenVariantSkus)
    {
        if (!seenModelCodes.Add(plan.ModelCode))
        {
            return Result.Failure(
                Error.Conflict($"Duplicate model code '{plan.ModelCode}' in batch request."));
        }

        foreach (var item in plan.Items)
        {
            if (!seenBarcodes.Add(item.Barcode))
            {
                return Result.Failure(
                    Error.Conflict($"Duplicate barcode '{item.Barcode}' in batch request."));
            }

            if (!string.IsNullOrWhiteSpace(item.Sku) && !seenVariantSkus.Add(item.Sku))
            {
                return Result.Failure(
                    Error.Conflict($"Duplicate variant SKU '{item.Sku}' in batch request."));
            }
        }

        return Result.Success();
    }

    private sealed record PlanEntry(
        ProductCreatePlan Plan,
        Guid CategoryId,
        ProductStatus Status,
        IReadOnlyList<AttributeValue> AttributeValues,
        Guid? BrandId,
        string? Description);
}
