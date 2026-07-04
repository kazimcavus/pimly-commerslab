using Channels.Application.Connections;
using Channels.Application.ExternalCatalog;
using Channels.Application.Imports.Catalog;
using Channels.Application.Imports.Planning;
using Channels.Application.Options;
using Channels.Domain;
using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.Connections;
using Channels.Domain.ExternalCatalog;
using Channels.Domain.Imports;
using Channels.Domain.Marketplaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel;
using SharedKernel.Tenancy;

namespace Channels.Application.Imports.ProcessProductImport;

/// <summary>
/// Claim edilmiş ürün import run'ını işler: pazaryerinden ürünleri çeker, kategori/özellik/varyantları
/// Catalog'da garanti eder, kanal eşlemelerini otomatik doldurur ve ürün gruplarını oluşturur.
/// </summary>
/// <remarks>
/// <para><b>Amaç:</b> Onboarding "ürünlerimi çek" akışının worker tarafı; gönderim (publish) fazına
/// zemin olacak eşlemeleri de import sırasında kurar.</para>
/// <para><b>Ön koşullar:</b> Run, kuyruktan claim edilip Running durumuna alınmış olmalı; ambient
/// tenant bağlamı run'ın tenant'ına set edilmiş olmalı (Catalog yazmaları tenant'ı buradan alır);
/// taksonomi cache'i (external_categories) senkronize edilmiş olmalı.</para>
/// <para><b>Ana akış:</b> Ürün sayfaları çekilir → kategori attribute cache'i tazelenir →
/// <see cref="ProductImportPlanner"/> ile plan kurulur → kategori zinciri + eksen/özellik tanımları +
/// kanal eşlemeleri garanti edilir → grup başına ürün oluşturulur, kanal fiyatı ve görseller yazılır →
/// ilerleme periyodik kaydedilir → run tamamlanır.</para>
/// <para><b>Hata durumları:</b> Altyapı hataları (bağlantı/istemci) run'ı Failed yapar; grup düzeyi
/// hatalar run hatası olarak kaydedilip diğer gruplarla devam edilir (completed_with_errors).</para>
/// <para><b>API:</b> Yalnızca worker (ProductImportBackgroundService) tarafından kullanılır.</para>
/// </remarks>
public sealed class ProcessProductImportHandler(
    IProductImportRunRepository importRuns,
    IMarketplaceConnectionRepository connections,
    IExternalCategoryRepository externalCategories,
    IExternalCategoryAttributeRepository externalAttributes,
    ICategoryChannelMappingRepository categoryMappings,
    IAttributeChannelMappingRepository attributeMappings,
    IAttributeValueChannelMappingRepository valueMappings,
    IMarketplaceProductsClientResolver productsClientResolver,
    IMarketplaceCategoryAttributesClientResolver attributesClientResolver,
    ICatalogImportGateway catalog,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork,
    IOptions<ProductImportOptions> options,
    TimeProvider timeProvider,
    ILogger<ProcessProductImportHandler> logger) : IProcessProductImportHandler
{
    /// <inheritdoc/>
    public async Task<Result> ExecuteAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var run = await importRuns.GetByIdAsync(runId, cancellationToken);
        if (run is null)
        {
            return Result.Failure(Error.NotFound("Product import run not found."));
        }

        if (run.Status != ProductImportStatus.Running)
        {
            return Result.Failure(Error.Conflict("Product import run is not in running state."));
        }

        if (tenantContext.TenantId != run.TenantId)
        {
            return Result.Failure(Error.Conflict("Tenant context does not match the import run."));
        }

        var marketplace = run.Marketplace;

        var productsClientResult = productsClientResolver.Resolve(marketplace);
        var attributesClientResult = attributesClientResolver.Resolve(marketplace);
        if (productsClientResult.IsFailure || attributesClientResult.IsFailure)
        {
            await FailRunAsync(run, "Marketplace clients are not configured.", cancellationToken);
            return Result.Success();
        }

        var connection = await connections.GetByMarketplaceAsync(marketplace, cancellationToken);
        if (connection is null || !connection.IsEnabled)
        {
            await FailRunAsync(run, "Marketplace connection is missing or disabled.", cancellationToken);
            return Result.Success();
        }

        var credentials = new MarketplaceCredentials(connection.SellerId, connection.ApiKey, connection.ApiSecret);

        try
        {
            var rowsResult = await FetchAllProductsAsync(productsClientResult.Value, marketplace, credentials, cancellationToken);
            if (rowsResult.IsFailure)
            {
                await FailRunAsync(run, rowsResult.Error.Message, cancellationToken);
                return Result.Success();
            }

            var rows = rowsResult.Value;
            var defsByCategory = await BuildAttributeDefsAsync(attributesClientResult.Value, marketplace, rows, cancellationToken);
            var plan = ProductImportPlanner.BuildPlan(rows, defsByCategory);

            var progress = new ImportProgress(plan.Groups.Count);
            run.UpdateProgress(0, 0, 0, 0, plan.Groups.Count);
            await SaveRunAsync(run, cancellationToken);

            var context = new ImportContext(marketplace, run.TenantId);

            foreach (var group in plan.Groups)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await ImportGroupAsync(run, context, group, progress, cancellationToken);

                progress.Processed++;
                if (progress.Processed % Math.Max(1, options.Value.ImportProgressSaveEveryGroups) == 0)
                {
                    ApplyProgress(run, progress);
                    await SaveRunAsync(run, cancellationToken);
                }
            }

            ApplyProgress(run, progress);
            var completeResult = run.MarkCompleted(timeProvider.GetUtcNow());
            if (completeResult.IsFailure)
            {
                return completeResult;
            }

            await SaveRunAsync(run, cancellationToken);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Product import {RunId} finished for tenant {TenantId}: {Imported} imported, {Skipped} skipped, {Failed} failed.",
                    run.Id,
                    run.TenantId,
                    progress.Imported,
                    progress.Skipped,
                    progress.Failed);
            }

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Product import {RunId} failed for tenant {TenantId}.", run.Id, run.TenantId);
            await FailRunAsync(run, ex.Message, cancellationToken);
            return Result.Success();
        }
    }

    private async Task ImportGroupAsync(
        ProductImportRun run,
        ImportContext context,
        ProductGroupPlan group,
        ImportProgress progress,
        CancellationToken cancellationToken)
    {
        foreach (var warning in group.Warnings)
        {
            run.AddError(group.ProductMainId, null, $"Uyarı: {warning}");
        }

        if (group.Error is not null)
        {
            progress.Failed++;
            run.AddError(group.ProductMainId, null, group.Error);
            return;
        }

        try
        {
            var categorySetupResult = await EnsureCategorySetupAsync(context, group.ExternalCategoryId, cancellationToken);
            if (categorySetupResult.IsFailure)
            {
                progress.Failed++;
                run.AddError(group.ProductMainId, null, categorySetupResult.Error.Message);
                return;
            }

            var categorySetup = categorySetupResult.Value;

            var barcodes = group.Items.Select(item => item.Barcode).ToList();
            if (await catalog.ProductGroupExistsAsync(group.ModelCode, barcodes, cancellationToken))
            {
                progress.Skipped++;
                return;
            }

            var buildResult = await BuildBatchInputAsync(context, categorySetup, group, cancellationToken);
            if (buildResult.IsFailure)
            {
                progress.Failed++;
                run.AddError(group.ProductMainId, null, buildResult.Error.Message);
                return;
            }

            var createResult = await catalog.CreateProductsBatchAsync(buildResult.Value, cancellationToken);
            if (createResult.IsFailure)
            {
                progress.Failed++;
                run.AddError(group.ProductMainId, null, createResult.Error.Message);
                return;
            }

            await WriteChannelPricesAsync(context, group, createResult.Value, run, cancellationToken);
            await AttachImagesAsync(group, createResult.Value, run, cancellationToken);

            progress.Imported++;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            progress.Failed++;
            run.AddError(group.ProductMainId, null, ex.Message);
            logger.LogWarning(ex, "Product group {ProductMainId} failed during import run {RunId}.", group.ProductMainId, run.Id);
        }
    }

    private async Task<Result<IReadOnlyList<MarketplaceProductNode>>> FetchAllProductsAsync(
        IMarketplaceProductsClient productsClient,
        Marketplace marketplace,
        MarketplaceCredentials credentials,
        CancellationToken cancellationToken)
    {
        var rows = new List<MarketplaceProductNode>();
        var page = 0;
        var pageSize = Math.Max(1, options.Value.ImportPageSize);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pageResult = await productsClient.FetchProductsPageAsync(
                marketplace,
                credentials,
                page,
                pageSize,
                cancellationToken);

            if (pageResult.IsFailure)
            {
                return Result.Failure<IReadOnlyList<MarketplaceProductNode>>(pageResult.Error);
            }

            rows.AddRange(pageResult.Value.Items);

            page++;
            if (page >= pageResult.Value.TotalPages || pageResult.Value.Items.Count == 0)
            {
                break;
            }
        }

        return Result.Success<IReadOnlyList<MarketplaceProductNode>>(rows);
    }

    private async Task<Dictionary<string, IReadOnlyList<ProductImportAttributeDef>>> BuildAttributeDefsAsync(
        IMarketplaceCategoryAttributesClient attributesClient,
        Marketplace marketplace,
        IReadOnlyList<MarketplaceProductNode> rows,
        CancellationToken cancellationToken)
    {
        var defsByCategory = new Dictionary<string, IReadOnlyList<ProductImportAttributeDef>>(StringComparer.Ordinal);

        foreach (var externalCategoryId in rows
                     .Select(row => row.ExternalCategoryId)
                     .Where(id => !string.IsNullOrWhiteSpace(id))
                     .Distinct(StringComparer.Ordinal))
        {
            var refreshResult = await ExternalCategoryAttributeCacheSupport.RefreshAsync(
                attributesClient,
                externalAttributes,
                marketplace,
                externalCategoryId,
                timeProvider.GetUtcNow(),
                cancellationToken);

            if (refreshResult.IsFailure)
            {
                logger.LogWarning(
                    "Category attribute cache refresh failed for {ExternalCategoryId}: {Error}. Falling back to cached definitions.",
                    externalCategoryId,
                    refreshResult.Error.Message);
            }
            else
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            var cached = await externalAttributes.ListByCategoryAsync(
                marketplace,
                externalCategoryId,
                cancellationToken);

            defsByCategory[externalCategoryId] = cached
                .Select(attribute => new ProductImportAttributeDef(
                    attribute.ExternalAttributeId,
                    attribute.Name,
                    attribute.Required,
                    attribute.AllowCustom,
                    attribute.IsVariant,
                    attribute.IsSlicer))
                .ToList();
        }

        return defsByCategory;
    }

    private async Task<Result<CategorySetup>> EnsureCategorySetupAsync(
        ImportContext context,
        string externalCategoryId,
        CancellationToken cancellationToken)
    {
        if (context.Categories.TryGetValue(externalCategoryId, out var cached))
        {
            return Result.Success(cached);
        }

        var externalCategory = await externalCategories.GetByExternalIdAsync(
            context.Marketplace,
            externalCategoryId,
            cancellationToken);

        if (externalCategory is null)
        {
            return Result.Failure<CategorySetup>(Error.NotFound(
                $"Pazaryeri kategorisi cache'te yok (id: {externalCategoryId}). Önce kategori senkronizasyonu çalıştırılmalı."));
        }

        // Dedup önce EŞLEME üzerinden: bu dış kategori daha önce bir catalog kategorisine
        // eşlendiyse, kullanıcı kategoriyi ağaçta taşımış/yeniden adlandırmış olsa bile
        // aynı kategori yeniden kullanılır (mükerrer "Halı" oluşmaz).
        var existingMapping = await categoryMappings.GetByExternalIdAsync(
            context.Marketplace,
            externalCategoryId,
            cancellationToken);

        if (existingMapping is not null)
        {
            if (await catalog.CategoryExistsAsync(existingMapping.CatalogCategoryId, cancellationToken))
            {
                var mappedSetup = new CategorySetup(existingMapping.CatalogCategoryId, externalCategoryId);
                context.Categories[externalCategoryId] = mappedSetup;
                return Result.Success(mappedSetup);
            }

            // Eşlenen kategori kullanıcı tarafından silinmiş: ölü eşlemeyi temizle, aşağıda yenisi kurulur.
            categoryMappings.Remove(existingMapping);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // Eşleme yoksa (ya da eşlenen kategori silinmişse): Shopify koleksiyonu gibi DÜZ model —
        // yalnızca yaprak kategori oluşturulur; Trendyol tarafındaki tam yol eşlemede
        // (ExternalCategory.Path) zaten bilinir, üst zincir Pimly ağacına kopyalanmaz.
        var leafResult = await catalog.EnsureCategoryPathAsync([externalCategory.Name], cancellationToken);
        if (leafResult.IsFailure)
        {
            return Result.Failure<CategorySetup>(leafResult.Error);
        }

        await UpsertCategoryMappingAsync(context, leafResult.Value, externalCategoryId, cancellationToken);

        var setup = new CategorySetup(leafResult.Value, externalCategoryId);
        context.Categories[externalCategoryId] = setup;
        return Result.Success(setup);
    }

    private async Task UpsertCategoryMappingAsync(
        ImportContext context,
        Guid catalogCategoryId,
        string externalCategoryId,
        CancellationToken cancellationToken)
    {
        var existing = await categoryMappings.GetAsync(context.Marketplace, catalogCategoryId, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.ExternalId, externalCategoryId, StringComparison.Ordinal))
            {
                existing.UpdateExternalCategory(externalCategoryId);
                categoryMappings.Update(existing);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        var createResult = CategoryChannelMapping.Create(
            context.TenantId,
            catalogCategoryId,
            context.Marketplace,
            externalCategoryId);

        if (createResult.IsSuccess)
        {
            await categoryMappings.AddAsync(createResult.Value, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<Result<CatalogProductBatchInput>> BuildBatchInputAsync(
        ImportContext context,
        CategorySetup categorySetup,
        ProductGroupPlan group,
        CancellationToken cancellationToken)
    {
        // Varyant eksenleri: global (tenant) düzeyde ada göre tekilleştirilir.
        var axisInputs = new List<CatalogVariantAxisInput>();
        var axisByExternalId = new Dictionary<string, EnsuredAxis>(StringComparer.Ordinal);

        foreach (var axis in group.VariantAxes)
        {
            var ensured = await EnsureAxisAsync(context, categorySetup, axis, cancellationToken);
            if (ensured.IsFailure)
            {
                return Result.Failure<CatalogProductBatchInput>(ensured.Error);
            }

            axisByExternalId[axis.ExternalAttributeId] = ensured.Value;
            axisInputs.Add(new CatalogVariantAxisInput(
                ensured.Value.VariantId,
                ensured.Value.IsColor,
                ensured.Value.Slicer));
        }

        // Ürün düzeyi özellik değerleri.
        var attributeSelections = new List<CatalogSelectionInput>();
        foreach (var attributeValue in group.AttributeValues)
        {
            var selection = await EnsureAttributeSelectionAsync(context, categorySetup, attributeValue, cancellationToken);
            if (selection.IsFailure)
            {
                return Result.Failure<CatalogProductBatchInput>(selection.Error);
            }

            attributeSelections.Add(selection.Value);
        }

        // Kalemler.
        var itemInputs = new List<CatalogProductItemInput>();
        foreach (var item in group.Items)
        {
            var variantSelections = new List<CatalogSelectionInput>();
            foreach (var selection in item.VariantSelections)
            {
                var axis = axisByExternalId[selection.ExternalAttributeId];
                var valueResult = await EnsureAxisValueAsync(context, axis, selection, cancellationToken);
                if (valueResult.IsFailure)
                {
                    return Result.Failure<CatalogProductBatchInput>(valueResult.Error);
                }

                variantSelections.Add(new CatalogSelectionInput(axis.VariantId, valueResult.Value));
            }

            var itemAttributeSelections = new List<CatalogSelectionInput>();
            foreach (var attributeValue in item.ItemAttributeValues)
            {
                var selection = await EnsureAttributeSelectionAsync(context, categorySetup, attributeValue, cancellationToken);
                if (selection.IsFailure)
                {
                    return Result.Failure<CatalogProductBatchInput>(selection.Error);
                }

                itemAttributeSelections.Add(selection.Value);
            }

            itemInputs.Add(new CatalogProductItemInput(
                item.Sku,
                item.Barcode,
                item.Price,
                item.CompareAtPrice,
                item.Stock,
                variantSelections,
                itemAttributeSelections));
        }

        return Result.Success(new CatalogProductBatchInput(
            Guid.NewGuid(),
            categorySetup.CatalogCategoryId,
            group.ModelCode,
            group.Name,
            Status: "active",
            attributeSelections,
            axisInputs,
            itemInputs,
            group.SplitOverrides
                .Select(split => new CatalogSplitInput(split.ValueName, split.StockCode, split.Title))
                .ToList()));
    }

    private async Task<Result<EnsuredAxis>> EnsureAxisAsync(
        ImportContext context,
        CategorySetup categorySetup,
        PlannedVariantAxis axis,
        CancellationToken cancellationToken)
    {
        if (!context.AxesByName.TryGetValue(axis.Name, out var ensured))
        {
            var variantResult = await catalog.EnsureVariantAsync(axis.Name, axis.IsColor, axis.Slicer, cancellationToken);
            if (variantResult.IsFailure)
            {
                return Result.Failure<EnsuredAxis>(variantResult.Error);
            }

            ensured = new EnsuredAxis(
                variantResult.Value.Id,
                variantResult.Value.IsColor,
                variantResult.Value.Slicer,
                new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase));
            context.AxesByName[axis.Name] = ensured;
        }

        await UpsertAttributeMappingAsync(
            context,
            categorySetup,
            AttributeMappingSourceType.CatalogVariant,
            ensured.VariantId,
            axis.ExternalAttributeId,
            cancellationToken);

        return Result.Success(ensured);
    }

    private async Task<Result<Guid>> EnsureAxisValueAsync(
        ImportContext context,
        EnsuredAxis axis,
        PlannedVariantSelection selection,
        CancellationToken cancellationToken)
    {
        if (!axis.ValueIdByLabel.TryGetValue(selection.ValueName, out var valueId))
        {
            var valueResult = await catalog.EnsureVariantValueAsync(axis.VariantId, selection.ValueName, cancellationToken);
            if (valueResult.IsFailure)
            {
                return valueResult;
            }

            valueId = valueResult.Value;
            axis.ValueIdByLabel[selection.ValueName] = valueId;
        }

        if (selection.ExternalValueId is not null)
        {
            await UpsertValueMappingAsync(context, axis.VariantId, valueId, selection.ExternalValueId, cancellationToken);
        }

        return Result.Success(valueId);
    }

    private async Task<Result<CatalogSelectionInput>> EnsureAttributeSelectionAsync(
        ImportContext context,
        CategorySetup categorySetup,
        PlannedAttributeValue attributeValue,
        CancellationToken cancellationToken)
    {
        if (!context.AttributesByName.TryGetValue(attributeValue.AttributeName, out var ensured))
        {
            var attributeResult = await catalog.EnsureAttributeAsync(attributeValue.AttributeName, cancellationToken);
            if (attributeResult.IsFailure)
            {
                return Result.Failure<CatalogSelectionInput>(attributeResult.Error);
            }

            ensured = new EnsuredAttribute(
                attributeResult.Value,
                new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase));
            context.AttributesByName[attributeValue.AttributeName] = ensured;
        }

        var assignmentKey = (categorySetup.CatalogCategoryId, ensured.AttributeId);
        if (context.AssignedAttributes.Add(assignmentKey))
        {
            var assignResult = await catalog.AssignAttributeToCategoryAsync(
                categorySetup.CatalogCategoryId,
                ensured.AttributeId,
                attributeValue.Required,
                sortOrder: context.AssignedAttributes.Count,
                cancellationToken);

            if (assignResult.IsFailure)
            {
                return Result.Failure<CatalogSelectionInput>(assignResult.Error);
            }
        }

        await UpsertAttributeMappingAsync(
            context,
            categorySetup,
            AttributeMappingSourceType.CatalogAttribute,
            ensured.AttributeId,
            attributeValue.ExternalAttributeId,
            cancellationToken);

        if (!ensured.ValueIdByName.TryGetValue(attributeValue.ValueName, out var valueId))
        {
            var valueResult = await catalog.EnsureAttributeValueAsync(
                ensured.AttributeId,
                attributeValue.ValueName,
                cancellationToken);

            if (valueResult.IsFailure)
            {
                return Result.Failure<CatalogSelectionInput>(valueResult.Error);
            }

            valueId = valueResult.Value;
            ensured.ValueIdByName[attributeValue.ValueName] = valueId;
        }

        if (attributeValue.ExternalValueId is not null)
        {
            await UpsertValueMappingAsync(context, ensured.AttributeId, valueId, attributeValue.ExternalValueId, cancellationToken);
        }

        return Result.Success(new CatalogSelectionInput(ensured.AttributeId, valueId));
    }

    private async Task UpsertAttributeMappingAsync(
        ImportContext context,
        CategorySetup categorySetup,
        AttributeMappingSourceType sourceType,
        Guid catalogSourceId,
        string externalAttributeId,
        CancellationToken cancellationToken)
    {
        var mappingKey = (categorySetup.CatalogCategoryId, sourceType, catalogSourceId);
        if (context.AttributeMappingIds.ContainsKey(mappingKey))
        {
            return;
        }

        var existing = await attributeMappings.GetAsync(
            context.Marketplace,
            categorySetup.CatalogCategoryId,
            sourceType,
            catalogSourceId,
            cancellationToken);

        if (existing is not null)
        {
            if (!string.Equals(existing.ExternalAttributeId, externalAttributeId, StringComparison.Ordinal))
            {
                existing.UpdateExternalAttribute(externalAttributeId);
                attributeMappings.Update(existing);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            context.AttributeMappingIds[mappingKey] = existing.Id;
            return;
        }

        var createResult = AttributeChannelMapping.Create(
            context.TenantId,
            context.Marketplace,
            categorySetup.CatalogCategoryId,
            sourceType,
            catalogSourceId,
            externalAttributeId);

        if (createResult.IsSuccess)
        {
            await attributeMappings.AddAsync(createResult.Value, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            context.AttributeMappingIds[mappingKey] = createResult.Value.Id;
        }
    }

    private async Task UpsertValueMappingAsync(
        ImportContext context,
        Guid catalogSourceId,
        Guid catalogValueId,
        string externalValueId,
        CancellationToken cancellationToken)
    {
        var mappingId = context.AttributeMappingIds
            .Where(pair => pair.Key.CatalogSourceId == catalogSourceId)
            .Select(pair => (Guid?)pair.Value)
            .FirstOrDefault();

        if (mappingId is null)
        {
            return;
        }

        var valueKey = (mappingId.Value, catalogValueId);
        if (!context.MappedValues.Add(valueKey))
        {
            return;
        }

        var existing = await valueMappings.GetAsync(mappingId.Value, catalogValueId, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.ExternalValueId, externalValueId, StringComparison.Ordinal))
            {
                existing.UpdateExternalValue(externalValueId);
                valueMappings.Update(existing);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        var createResult = AttributeValueChannelMapping.Create(
            context.TenantId,
            mappingId.Value,
            catalogValueId,
            externalValueId);

        if (createResult.IsSuccess)
        {
            await valueMappings.AddAsync(createResult.Value, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task WriteChannelPricesAsync(
        ImportContext context,
        ProductGroupPlan group,
        IReadOnlyList<CreatedProductSnapshot> createdProducts,
        ProductImportRun run,
        CancellationToken cancellationToken)
    {
        var itemsByBarcode = group.Items.ToDictionary(item => item.Barcode, StringComparer.OrdinalIgnoreCase);

        foreach (var product in createdProducts)
        {
            foreach (var (barcode, itemId) in product.ItemIdByBarcode)
            {
                if (!itemsByBarcode.TryGetValue(barcode, out var plannedItem))
                {
                    continue;
                }

                var priceResult = await catalog.UpsertItemChannelPriceAsync(
                    itemId,
                    context.Marketplace.Code,
                    plannedItem.Price,
                    plannedItem.CompareAtPrice,
                    plannedItem.Currency,
                    cancellationToken);

                if (priceResult.IsFailure)
                {
                    run.AddError(group.ProductMainId, barcode, $"Kanal fiyatı yazılamadı: {priceResult.Error.Message}");
                }
            }
        }
    }

    private async Task AttachImagesAsync(
        ProductGroupPlan group,
        IReadOnlyList<CreatedProductSnapshot> createdProducts,
        ProductImportRun run,
        CancellationToken cancellationToken)
    {
        var maxImages = Math.Max(0, options.Value.ImportMaxImagesPerProduct);
        if (maxImages == 0)
        {
            return;
        }

        foreach (var product in createdProducts)
        {
            // Ürüne ait ilk barkodun satırındaki görseller kullanılır (renk bölünmesinde renk görselleri).
            var firstBarcode = product.ItemIdByBarcode.Keys.FirstOrDefault();
            if (firstBarcode is null)
            {
                continue;
            }

            var plannedItem = group.Items.FirstOrDefault(item =>
                string.Equals(item.Barcode, firstBarcode, StringComparison.OrdinalIgnoreCase));

            if (plannedItem is null || plannedItem.ImageUrls.Count == 0)
            {
                continue;
            }

            var sortOrder = 0;
            foreach (var imageUrl in plannedItem.ImageUrls.Take(maxImages))
            {
                var imageResult = await catalog.AddProductImageAsync(
                    product.ProductId,
                    imageUrl,
                    sortOrder,
                    isPrimary: sortOrder == 0,
                    cancellationToken);

                if (imageResult.IsFailure)
                {
                    run.AddError(group.ProductMainId, firstBarcode, $"Görsel aktarılamadı: {imageResult.Error.Message}");
                    break;
                }

                sortOrder++;
            }
        }
    }

    private static void ApplyProgress(ProductImportRun run, ImportProgress progress) =>
        run.UpdateProgress(
            progress.Processed,
            progress.Imported,
            progress.Skipped,
            progress.Failed,
            progress.Total);

    private async Task SaveRunAsync(ProductImportRun run, CancellationToken cancellationToken)
    {
        // Update() burada aggregate'i yeniden Modified yapmaz; yalnızca yanlışlıkla Modified
        // işaretlenmiş yeni hata child'larını Added'e çevirir (bkz. ProductImportRunRepository).
        importRuns.Update(run);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task FailRunAsync(ProductImportRun run, string message, CancellationToken cancellationToken)
    {
        var failResult = run.MarkFailed(timeProvider.GetUtcNow(), message);
        if (failResult.IsFailure)
        {
            return;
        }

        await SaveRunAsync(run, cancellationToken);
    }

    private sealed class ImportProgress(int total)
    {
        public int Total { get; } = total;

        public int Processed { get; set; }

        public int Imported { get; set; }

        public int Skipped { get; set; }

        public int Failed { get; set; }
    }

    private sealed class ImportContext(Marketplace marketplace, Guid tenantId)
    {
        public Marketplace Marketplace { get; } = marketplace;

        public Guid TenantId { get; } = tenantId;

        public Dictionary<string, CategorySetup> Categories { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, EnsuredAxis> AxesByName { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, EnsuredAttribute> AttributesByName { get; } = new(StringComparer.OrdinalIgnoreCase);

        public HashSet<(Guid CategoryId, Guid AttributeId)> AssignedAttributes { get; } = [];

        public Dictionary<(Guid CategoryId, AttributeMappingSourceType SourceType, Guid CatalogSourceId), Guid> AttributeMappingIds { get; } = [];

        public HashSet<(Guid MappingId, Guid CatalogValueId)> MappedValues { get; } = [];
    }

    private sealed record CategorySetup(Guid CatalogCategoryId, string ExternalCategoryId);

    private sealed record EnsuredAxis(
        Guid VariantId,
        bool IsColor,
        bool Slicer,
        Dictionary<string, Guid> ValueIdByLabel);

    private sealed record EnsuredAttribute(
        Guid AttributeId,
        Dictionary<string, Guid> ValueIdByName);
}
