using Catalog.Domain.Products;
using Catalog.Domain.SkuGenerator;
using SharedKernel;

namespace Catalog.Application.SkuGenerator;

/// <summary>Ürün oluşturma sırasında SKU üretimini yönetir.</summary>
public interface ISkuGeneratorService
{
    Task<SkuGeneratorConfig?> GetConfigAsync(CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ProductCreatePlan>>> BuildPlansAsync(
        string modelCode,
        IReadOnlyList<string>? codeInputs,
        string name,
        IReadOnlyList<Variant> variants,
        IReadOnlyList<ProductItemDraft> drafts,
        IReadOnlyList<ProductSplitOverride>? splitOverrides = null,
        CancellationToken cancellationToken = default);
}

/// <inheritdoc/>
public sealed class SkuGeneratorService(
    ISkuGeneratorConfigRepository configs,
    ISkuCounterAllocator counterAllocator) : ISkuGeneratorService
{
    /// <inheritdoc/>
    public async Task<SkuGeneratorConfig?> GetConfigAsync(CancellationToken cancellationToken = default) =>
        await configs.GetAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<ProductCreatePlan>>> BuildPlansAsync(
        string modelCode,
        IReadOnlyList<string>? codeInputs,
        string name,
        IReadOnlyList<Variant> variants,
        IReadOnlyList<ProductItemDraft> drafts,
        IReadOnlyList<ProductSplitOverride>? splitOverrides = null,
        CancellationToken cancellationToken = default)
    {
        var config = await configs.GetAsync(cancellationToken);
        var useGenerator = config is { Enabled: true } && string.IsNullOrWhiteSpace(modelCode);

        if (useGenerator)
        {
            var manualValidation = SkuCodeAssembler.ValidateManualInputs(config!.Segments, codeInputs);
            if (manualValidation.IsFailure)
            {
                return Result.Failure<IReadOnlyList<ProductCreatePlan>>(manualValidation.Error);
            }

            foreach (var draft in drafts)
            {
                var variantValidation = ValidateDraftVariantCodes(config.Segments, draft);
                if (variantValidation.IsFailure)
                {
                    return Result.Failure<IReadOnlyList<ProductCreatePlan>>(variantValidation.Error);
                }
            }
        }
        else if (string.IsNullOrWhiteSpace(modelCode))
        {
            return Result.Failure<IReadOnlyList<ProductCreatePlan>>(
                Error.Validation("Model code is required."));
        }

        var baseForSplit = useGenerator ? SkuGeneratorConstants.BasePlaceholder : modelCode.Trim();
        var splitResult = ProductCreateSplitter.Split(baseForSplit, name, variants, drafts, splitOverrides);
        if (splitResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<ProductCreatePlan>>(splitResult.Error);
        }

        var counter = config?.CounterNextValue ?? 1;
        if (useGenerator)
        {
            config!.EnsureCounterInitialized();
            counter = config.CounterNextValue;

            var totalCounterUses = splitResult.Value.Count * config.CounterSegmentCount;
            if (totalCounterUses > 0)
            {
                var reserveResult = await counterAllocator.ReserveAsync(totalCounterUses, cancellationToken);
                if (reserveResult.IsFailure)
                {
                    return Result.Failure<IReadOnlyList<ProductCreatePlan>>(reserveResult.Error);
                }

                counter = reserveResult.Value;
            }
        }

        var finalPlans = new List<ProductCreatePlan>();
        foreach (var plan in splitResult.Value)
        {
            string finalModelCode;
            if (useGenerator)
            {
                var assembleResult = SkuCodeAssembler.AssembleProductCode(
                    config!.Segments,
                    codeInputs,
                    counter);

                if (assembleResult.IsFailure)
                {
                    return Result.Failure<IReadOnlyList<ProductCreatePlan>>(assembleResult.Error);
                }

                counter = assembleResult.Value.NextCounter;
                finalModelCode = plan.ModelCode.Replace(
                    SkuGeneratorConstants.BasePlaceholder,
                    assembleResult.Value.Code,
                    StringComparison.Ordinal);
            }
            else
            {
                finalModelCode = plan.ModelCode;
            }

            var items = plan.Items
                .Select(draft => ApplyVariantSku(config, useGenerator, finalModelCode, draft))
                .ToList();

            // Generator yolunda her plan sayaçtan kendi kodunu alır; paylaşılan grup kodu anlamsızdır.
            var groupCode = useGenerator ? null : plan.GroupCode;

            finalPlans.Add(plan with { ModelCode = finalModelCode, Items = items, GroupCode = groupCode });
        }

        return Result.Success<IReadOnlyList<ProductCreatePlan>>(finalPlans);
    }

    private static Result ValidateDraftVariantCodes(
        IReadOnlyList<SkuSegment> segments,
        ProductItemDraft draft)
    {
        var selections = (draft.VariantValues ?? [])
            .Select(value => new SkuVariantSelection(
                value.Variant.SelectionStyle,
                value.Name,
                value.Key))
            .ToList();

        return SkuCodeAssembler.ValidateVariantCodes(segments, selections);
    }

    private static ProductItemDraft ApplyVariantSku(
        SkuGeneratorConfig? config,
        bool useGenerator,
        string modelCode,
        ProductItemDraft draft)
    {
        if (!useGenerator || config is null || !string.IsNullOrWhiteSpace(draft.Sku))
        {
            return draft;
        }

        var selections = (draft.VariantValues ?? [])
            .Select(value => new SkuVariantSelection(
                value.Variant.SelectionStyle,
                value.Name,
                value.Key))
            .ToList();

        var sku = SkuCodeAssembler.AssembleVariantSku(modelCode, config.Segments, selections);
        return draft with { Sku = sku };
    }
}
