using Catalog.Application.Attributes.AddAttributeValue;
using Catalog.Application.Attributes.CreateAttribute;
using Catalog.Application.Brands.CreateBrand;
using Catalog.Application.Categories.AssignCategoryAttribute;
using Catalog.Application.Categories.CreateCategory;
using Catalog.Application.Products;
using Catalog.Application.Products.AddProductImage;
using Catalog.Application.Products.CreateProduct;
using Catalog.Application.Products.CreateProductsBatch;
using Catalog.Application.Variants.AddVariantValue;
using Catalog.Application.Variants.CreateVariantType;
using Catalog.Domain;
using Catalog.Domain.Products;
using Catalog.Domain.Variants;
using Channels.Application.ProductImports.Catalog;
using Media.Application.UploadImage;
using Pricing.Application.BasePrices.SetBasePrice;
using Pricing.Application.ItemPrices.UpsertItemPrice;
using Pricing.Application.PriceDefinitions.CreatePriceDefinition;
using SharedKernel;
using CatalogProductVariant = Catalog.Domain.Products.Variant;
using IPriceDefinitionRepository = Pricing.Domain.PriceDefinitions.IPriceDefinitionRepository;

namespace Pimly.ProductImports.Worker.Integration;

/// <summary>
/// Ürün import hattının Catalog yazma kapısı; Catalog handler ve repolarına delege eder.
/// API host'undaki okuma gateway'lerinin yazma tarafı karşılığıdır. Tüm işlemler idempotenttir;
/// tenant, ambient tenant bağlamından (Catalog DbContext stamping) akar.
/// </summary>
internal sealed class CatalogImportGateway(
    ICategoryRepository categories,
    IBrandRepository brands,
    IPriceDefinitionRepository priceDefinitions,
    IAttributeRepository attributes,
    IVariantRepository variants,
    IProductRepository products,
    ICreateCategoryHandler createCategory,
    ICreateBrandHandler createBrand,
    ICreatePriceDefinitionHandler createPriceDefinition,
    ICreateAttributeHandler createAttribute,
    IAddAttributeValueHandler addAttributeValue,
    ICreateVariantTypeHandler createVariantType,
    IAddVariantValueHandler addVariantValue,
    IAssignCategoryAttributeHandler assignCategoryAttribute,
    ICreateProductsBatchHandler createProductsBatch,
    IAddProductImageHandler addProductImage,
    IUploadImageHandler uploadImage,
    IUpsertItemPriceHandler upsertItemPrice,
    ISetBasePriceHandler setBasePrice,
    IHttpClientFactory httpClientFactory) : ICatalogImportGateway
{
    private const long MaxImageBytes = 10 * 1024 * 1024;

    /// <inheritdoc/>
    public async Task<Result<Guid>> EnsureCategoryPathAsync(
        IReadOnlyList<string> pathSegments,
        CancellationToken cancellationToken = default)
    {
        if (pathSegments.Count == 0)
        {
            return Result.Failure<Guid>(Error.Validation("Category path is required."));
        }

        var existing = await categories.ListAsync(cancellationToken);
        var byParentAndName = existing
            .GroupBy(category => (category.ParentId, Name: category.Name.Trim().ToLowerInvariant()))
            .ToDictionary(group => group.Key, group => group.First().Id);

        Guid? parentId = null;
        foreach (var segment in pathSegments)
        {
            var key = (parentId, segment.Trim().ToLowerInvariant());
            if (byParentAndName.TryGetValue(key, out var categoryId))
            {
                parentId = categoryId;
                continue;
            }

            var createResult = await createCategory.ExecuteAsync(
                new CreateCategoryCommand(segment.Trim(), Code: null, parentId),
                cancellationToken);

            if (createResult.IsFailure)
            {
                return Result.Failure<Guid>(createResult.Error);
            }

            parentId = createResult.Value.Id;
            byParentAndName[key] = createResult.Value.Id;
        }

        return Result.Success(parentId!.Value);
    }

    /// <inheritdoc/>
    public async Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
        await categories.GetByIdAsync(categoryId, cancellationToken) is not null;

    /// <inheritdoc/>
    public async Task<Result<Guid>> EnsureAttributeAsync(string name, CancellationToken cancellationToken = default)
    {
        var existing = await attributes.ListAsync(cancellationToken);
        var match = existing.FirstOrDefault(attribute =>
            string.Equals(attribute.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));

        if (match is not null)
        {
            return Result.Success(match.Id);
        }

        var createResult = await createAttribute.ExecuteAsync(new CreateAttributeCommand(name.Trim()), cancellationToken);
        return createResult.IsFailure
            ? Result.Failure<Guid>(createResult.Error)
            : Result.Success(createResult.Value.Id);
    }

    /// <inheritdoc/>
    public async Task<Result<Guid>> EnsureBrandAsync(
        string name,
        string? externalId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Guid>(Error.Validation("Brand name is required."));
        }

        var existing = await brands.GetByNameAsync(name.Trim(), cancellationToken);
        if (existing is not null)
        {
            return Result.Success(existing.Id);
        }

        var createResult = await createBrand.ExecuteAsync(
            new CreateBrandCommand(name.Trim(), externalId),
            cancellationToken);

        return createResult.IsFailure
            ? Result.Failure<Guid>(createResult.Error)
            : Result.Success(createResult.Value.Id);
    }

    /// <inheritdoc/>
    public async Task<Result<Guid>> EnsureAttributeValueAsync(
        Guid attributeId,
        string valueName,
        CancellationToken cancellationToken = default)
    {
        var attribute = await attributes.GetByIdAsync(attributeId, cancellationToken);
        if (attribute is null)
        {
            return Result.Failure<Guid>(Error.NotFound("Attribute not found."));
        }

        var match = attribute.Values.FirstOrDefault(value =>
            string.Equals(value.Name, valueName.Trim(), StringComparison.OrdinalIgnoreCase));

        if (match is not null)
        {
            return Result.Success(match.Id);
        }

        var addResult = await addAttributeValue.ExecuteAsync(
            new AddAttributeValueCommand(attributeId, valueName.Trim()),
            cancellationToken);

        return addResult.IsFailure
            ? Result.Failure<Guid>(addResult.Error)
            : Result.Success(addResult.Value.Id);
    }

    /// <inheritdoc/>
    public async Task<Result<EnsuredVariantSnapshot>> EnsureVariantAsync(
        string name,
        bool isColor,
        bool slicer,
        CancellationToken cancellationToken = default)
    {
        var existing = await variants.GetByNameAsync(name.Trim(), cancellationToken);
        if (existing is not null)
        {
            return Result.Success(new EnsuredVariantSnapshot(
                existing.Id,
                existing.Name,
                existing.SelectionStyle == SelectionStyle.Color,
                existing.Slicer,
                SlicerDemoted: slicer && !existing.Slicer));
        }

        // Tenant başına tek slicer ekseni kuralı: başka bir slicer varsa eksen slicer'sız açılır.
        var slicerDemoted = false;
        if (slicer)
        {
            var currentSlicer = await variants.GetSlicerVariantAsync(excludeId: null, cancellationToken);
            if (currentSlicer is not null)
            {
                slicer = false;
                slicerDemoted = true;
            }
        }

        var createResult = await createVariantType.ExecuteAsync(
            new CreateVariantTypeCommand(
                name.Trim(),
                isColor ? "color" : "list",
                SortOrder: 0,
                Slicer: slicer),
            cancellationToken);

        if (createResult.IsFailure)
        {
            return Result.Failure<EnsuredVariantSnapshot>(createResult.Error);
        }

        return Result.Success(new EnsuredVariantSnapshot(
            createResult.Value.Id,
            createResult.Value.Name,
            isColor,
            createResult.Value.Slicer,
            slicerDemoted));
    }

    /// <inheritdoc/>
    public async Task<Result<Guid>> EnsureVariantValueAsync(
        Guid variantId,
        string label,
        CancellationToken cancellationToken = default)
    {
        var variant = await variants.GetByIdAsync(variantId, cancellationToken);
        if (variant is null)
        {
            return Result.Failure<Guid>(Error.NotFound("Variant type not found."));
        }

        // Etiket eşleşmesi VEYA aynı slug-anahtara indirgenen mevcut değer → yeniden kullan.
        // Trendyol'da yalnızca boşluk/noktalama ile ayrışan değerler (ör. "80x200" ↔ "80 x 200")
        // aynı anahtarı üretir; bunları ayrı değer olarak eklemeye çalışmak anahtar çakışması
        // ("Variant value key must be unique") verip ürünü hataya sokardı.
        var trimmed = label.Trim();
        var previewKey = VariantKey.TryPreview(trimmed);
        var match = variant.Values.FirstOrDefault(value =>
            string.Equals(value.Label, trimmed, StringComparison.OrdinalIgnoreCase)
            || (previewKey is not null && string.Equals(value.Key.Value, previewKey, StringComparison.OrdinalIgnoreCase)));

        if (match is not null)
        {
            return Result.Success(match.Id);
        }

        var addResult = await addVariantValue.ExecuteAsync(
            new AddVariantValueCommand(variantId, trimmed, Color: null, ImageUrl: null, Key: null, SortOrder: 0),
            cancellationToken);

        return addResult.IsFailure
            ? Result.Failure<Guid>(addResult.Error)
            : Result.Success(addResult.Value.Id);
    }

    /// <inheritdoc/>
    public async Task<Result> AssignAttributeToCategoryAsync(
        Guid categoryId,
        Guid attributeId,
        bool required,
        int sortOrder,
        CancellationToken cancellationToken = default)
    {
        var category = await categories.GetByIdAsync(categoryId, cancellationToken);
        if (category is null)
        {
            return Result.Failure(Error.NotFound("Category not found."));
        }

        if (category.Assignments.Any(assignment => assignment.AttributeId == attributeId))
        {
            return Result.Success();
        }

        var assignResult = await assignCategoryAttribute.ExecuteAsync(
            new AssignCategoryAttributeCommand(categoryId, attributeId, required, sortOrder),
            cancellationToken);

        // Eşzamanlı atama çakışması idempotentlik açısından başarı sayılır.
        if (assignResult.IsFailure && assignResult.Error.Code == ErrorCodes.Conflict)
        {
            return Result.Success();
        }

        return assignResult.IsFailure ? Result.Failure(assignResult.Error) : Result.Success();
    }

    /// <inheritdoc/>
    public async Task<bool> ProductGroupExistsAsync(
        string modelCode,
        IReadOnlyList<string> barcodes,
        CancellationToken cancellationToken = default)
    {
        if (await products.ModelCodeExistsAsync(modelCode, cancellationToken))
        {
            return true;
        }

        foreach (var barcode in barcodes)
        {
            if (await products.BarcodeExistsAsync(barcode, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<CreatedProductSnapshot>>> CreateProductsBatchAsync(
        CatalogProductBatchInput input,
        CancellationToken cancellationToken = default)
    {
        var splitOverrides = input.Splits?
            .Select(split => new ProductSplitOverride(split.ValueName, split.ModelCode, split.Name, split.Description))
            .ToList();

        var batchItem = new CreateProductsBatchItem(
            input.CategoryId,
            input.ModelCode,
            input.Name,
            input.Status,
            CodeInputs: null,
            input.AttributeValues
                .Select(selection => new AttributeValueInput(selection.Id, selection.ValueId))
                .ToList(),
            input.Variants
                .Select(axis => new CatalogProductVariant(
                    axis.VariantId,
                    string.Empty,
                    axis.IsColor ? SelectionStyle.Color : SelectionStyle.List,
                    axis.Slicer))
                .ToList(),
            input.Items
                .Select(item => new CreateProductItemInput(
                    item.Sku,
                    item.Barcode,
                    Gtin: null,
                    Mpn: null,
                    AxisValueEntryId: null,
                    AxisValue: null,
                    item.Stock,
                    item.AttributeValues
                        .Select(selection => new AttributeValueInput(selection.Id, selection.ValueId))
                        .ToList(),
                    item.VariantValues
                        .Select(selection => new VariantValueInput(selection.Id, selection.ValueId))
                        .ToList()))
                .ToList(),
            splitOverrides,
            input.BrandId,
            input.Description);

        var createResult = await createProductsBatch.ExecuteAsync(
            new CreateProductsBatchCommand(input.GroupId, [batchItem]),
            cancellationToken);

        if (createResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<CreatedProductSnapshot>>(createResult.Error);
        }

        IReadOnlyList<CreatedProductSnapshot> snapshots = createResult.Value.Products
            .Select(product => new CreatedProductSnapshot(
                product.Id,
                product.Items.ToDictionary(
                    item => item.Barcode,
                    item => item.Id,
                    StringComparer.OrdinalIgnoreCase)))
            .ToList();

        // Temel fiyat, kalem Catalog'da oluşturulduktan sonra Pricing'e yazılır (dual-write).
        // Catalog.product_items.price kolonu contract dilimine kadar dormant olarak da doldurulur.
        var itemIdByBarcode = snapshots
            .SelectMany(snapshot => snapshot.ItemIdByBarcode)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

        foreach (var item in input.Items)
        {
            if (!itemIdByBarcode.TryGetValue(item.Barcode, out var itemId))
            {
                continue;
            }

            var basePriceResult = await setBasePrice.ExecuteAsync(
                new SetBasePriceCommand(itemId, item.Price, item.CompareAtPrice),
                cancellationToken);

            if (basePriceResult.IsFailure)
            {
                return Result.Failure<IReadOnlyList<CreatedProductSnapshot>>(basePriceResult.Error);
            }
        }

        return Result.Success(snapshots);
    }

    /// <inheritdoc/>
    public async Task<Result> AddProductImageAsync(
        Guid productId,
        string sourceUrl,
        int sortOrder,
        bool isPrimary,
        CancellationToken cancellationToken = default)
    {
        // Harici görsel medya deposuna alınır; ProductImage doğrulaması yalnızca /media/ URL kabul eder.
        var httpClient = httpClientFactory.CreateClient(nameof(CatalogImportGateway));

        var downloadResult = await DownloadImageWithRetryAsync(httpClient, sourceUrl, cancellationToken);
        if (downloadResult.IsFailure)
        {
            return Result.Failure(downloadResult.Error);
        }

        var bytes = downloadResult.Value;

        if (bytes.LongLength > MaxImageBytes)
        {
            return Result.Failure(Error.Validation("Image exceeds the allowed size."));
        }

        await using var stream = new MemoryStream(bytes);
        var uploadResult = await uploadImage.ExecuteAsync(
            new UploadImageCommand(stream, bytes.LongLength, UploadPurpose.Product),
            cancellationToken);

        if (uploadResult.IsFailure)
        {
            return Result.Failure(uploadResult.Error);
        }

        var addResult = await addProductImage.ExecuteAsync(
            new AddProductImageCommand(
                productId,
                uploadResult.Value.Url,
                sortOrder,
                AltText: null,
                isPrimary,
                VariantValueId: null),
            cancellationToken);

        return addResult.IsFailure ? Result.Failure(addResult.Error) : Result.Success();
    }

    // Trendyol CDN'i ara sıra geçici DNS/timeout hatası verir; artan beklemeyle en fazla
    // 3 deneme yapılır (1s, 3s). Kalıcı hata ve gerçek iptal aynen üst katmana taşınır.
    private static readonly TimeSpan[] ImageRetryDelays = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3)];

    private static async Task<Result<byte[]>> DownloadImageWithRetryAsync(
        HttpClient httpClient,
        string sourceUrl,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                using var response = await httpClient.GetAsync(sourceUrl, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return Result.Failure<byte[]>(Error.Failure($"Image download failed with status {(int)response.StatusCode}."));
                }

                if (response.Content.Headers.ContentLength is > MaxImageBytes)
                {
                    return Result.Failure<byte[]>(Error.Validation("Image exceeds the allowed size."));
                }

                return Result.Success(await response.Content.ReadAsByteArrayAsync(cancellationToken));
            }
            catch (Exception ex)
                when ((ex is HttpRequestException or TaskCanceledException)
                    && !cancellationToken.IsCancellationRequested
                    && attempt < ImageRetryDelays.Length)
            {
                await Task.Delay(ImageRetryDelays[attempt], cancellationToken);
            }
            catch (Exception ex)
                when ((ex is HttpRequestException or TaskCanceledException)
                    && !cancellationToken.IsCancellationRequested)
            {
                return Result.Failure<byte[]>(Error.Failure($"Image download failed: {ex.Message}"));
            }
        }
    }

    /// <inheritdoc/>
    public async Task<Result<Guid>> EnsurePriceDefinitionAsync(
        string name,
        string? code,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Guid>(Error.Validation("Price definition name is required."));
        }

        var existing = await priceDefinitions.GetByNameAsync(name.Trim(), cancellationToken);
        if (existing is not null)
        {
            return Result.Success(existing.Id);
        }

        var createResult = await createPriceDefinition.ExecuteAsync(
            new CreatePriceDefinitionCommand(name.Trim(), code),
            cancellationToken);

        return createResult.IsFailure
            ? Result.Failure<Guid>(createResult.Error)
            : Result.Success(createResult.Value.Id);
    }

    /// <inheritdoc/>
    public async Task<Result> UpsertItemPriceAsync(
        Guid productItemId,
        Guid priceDefinitionId,
        decimal amount,
        string? currency,
        CancellationToken cancellationToken = default)
    {
        var upsertResult = await upsertItemPrice.ExecuteAsync(
            new UpsertItemPriceCommand(productItemId, priceDefinitionId, amount, currency),
            cancellationToken);

        return upsertResult.IsFailure ? Result.Failure(upsertResult.Error) : Result.Success();
    }
}
