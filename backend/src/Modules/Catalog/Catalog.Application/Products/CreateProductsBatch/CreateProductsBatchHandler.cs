using Catalog.Application.Barcodes;
using Catalog.Application.Contracts;
using Catalog.Application.SkuGenerator;
using Catalog.Application.Validation;
using Catalog.Domain;
using Catalog.Domain.Products;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Products.CreateProductsBatch;

/// <summary>Toplu ürün oluşturma işlemini yürüten handler.</summary>
public sealed class CreateProductsBatchHandler(
    IValidator<CreateProductsBatchCommand> validator,
    IProductRepository products,
    IVariantRepository variantTypes,
    IAttributeRepository attributes,
    ISkuGeneratorService skuGenerator,
    IBarcodeAllocator barcodeAllocator,
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

        foreach (var item in command.Products)
        {
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

            // Boş bırakılan barkodları barkod serisinden otomatik tahsis et.
            var barcodeFillResult = await FillMissingBarcodesAsync(
                itemDraftsResult.Value,
                cancellationToken);

            if (barcodeFillResult.IsFailure)
            {
                return Result.Failure<CreateProductsBatchResult>(barcodeFillResult.Error);
            }

            var plansResult = await skuGenerator.BuildPlansAsync(
                item.ModelCode,
                item.CodeInputs,
                item.Name,
                resolvedTypesResult.Value,
                barcodeFillResult.Value,
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
                    ProductMappings.ParseStatus(item.Status),
                    attributeValuesResult.Value));
            }
        }

        var createdProducts = new List<Product>();
        foreach (var entry in planEntries)
        {
            var createResult = Product.Create(
                command.GroupId,
                entry.Plan.ModelCode,
                entry.Plan.Name,
                entry.Status,
                entry.AttributeValues,
                entry.Plan.Variants,
                entry.Plan.Items.ToList());

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

    // Barkodu boş olan kalemlere seriden sıradaki barkodları atar (atomik tahsis;
    // seri yapılandırılmamışsa anlaşılır bir hata döner).
    private async Task<Result<IReadOnlyList<ProductItemDraft>>> FillMissingBarcodesAsync(
        IReadOnlyList<ProductItemDraft> drafts,
        CancellationToken cancellationToken)
    {
        var missingCount = drafts.Count(draft => string.IsNullOrWhiteSpace(draft.Barcode));
        if (missingCount == 0)
        {
            return Result.Success(drafts);
        }

        var allocationResult = await barcodeAllocator.AllocateAsync(missingCount, cancellationToken);
        if (allocationResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<ProductItemDraft>>(allocationResult.Error);
        }

        var barcodes = new Queue<string>(allocationResult.Value.Select(allocated => allocated.Barcode));
        var filled = drafts
            .Select(draft => string.IsNullOrWhiteSpace(draft.Barcode)
                ? draft with { Barcode = barcodes.Dequeue() }
                : draft)
            .ToList();

        return Result.Success<IReadOnlyList<ProductItemDraft>>(filled);
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
        ProductStatus Status,
        IReadOnlyList<AttributeValue> AttributeValues);
}
