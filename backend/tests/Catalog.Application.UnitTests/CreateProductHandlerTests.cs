using Catalog.Application.Products;
using Catalog.Application.Products.CreateProduct;
using Catalog.Application.Products.CreateProductsBatch;
using Catalog.Application.SkuGenerator;
using Catalog.Domain;
using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using FluentAssertions;
using Moq;
using SharedKernel;
using AttributeDefinition = Catalog.Domain.Attributes.Attribute;

namespace Catalog.Application.UnitTests;

/// <summary>CreateProductHandler için zorunlu kategori özniteliği testleri.</summary>
public class CreateProductHandlerTests
{
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IBrandRepository> _brands = new();
    private readonly Mock<IVariantRepository> _variants = new();
    private readonly Mock<IAttributeRepository> _attributes = new();
    private readonly Mock<ISkuGeneratorService> _skuGenerator = new();

    public CreateProductHandlerTests()
    {
        HandlerTestSupport.SetupPassthroughPlans(_skuGenerator);
    }

    [Fact]
    public async Task ExecuteAsync_RequiredCategoryAttributeMissing_ReturnsValidationError()
    {
        var (category, attribute) = HandlerTestSupport.CategoryWithRequiredAttribute();
        _categories
            .Setup(c => c.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        _attributes
            .Setup(a => a.GetByIdAsync(attribute.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        var handler = CreateHandler();

        var result = await handler.ExecuteAsync(BuildCommand(category.Id, attributeValues: null));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Validation);
        result.Error.Message.Should().Be($"Required attribute missing: {attribute.Name}");
        _products.Verify(p => p.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_RequiredCategoryAttributeProvided_Succeeds()
    {
        var (category, attribute) = HandlerTestSupport.CategoryWithRequiredAttribute();
        var attributeValue = attribute.Values.Single();
        _categories
            .Setup(c => c.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        _attributes
            .Setup(a => a.GetByIdAsync(attribute.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        var handler = CreateHandler();

        var result = await handler.ExecuteAsync(BuildCommand(
            category.Id,
            [new AttributeValueInput(attribute.Id, attributeValue.Id)]));

        result.IsSuccess.Should().BeTrue();
        result.Value.AttributeValues.Should().ContainSingle(value => value.Attribute.Id == attribute.Id);
        _products.Verify(p => p.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static CreateProductCommand BuildCommand(
        Guid categoryId,
        IReadOnlyList<AttributeValueInput>? attributeValues) =>
        new(
            Guid.NewGuid(),
            categoryId,
            "SKU-001",
            "Title",
            "draft",
            null,
            attributeValues,
            [],
            [HandlerTestSupport.ValidItem()]);

    private CreateProductHandler CreateHandler() =>
        new(
            new CreateProductCommandValidator(),
            _products.Object,
            _categories.Object,
            _brands.Object,
            _variants.Object,
            _attributes.Object,
            _skuGenerator.Object,
            Mock.Of<IUnitOfWork>());
}

/// <summary>CreateProductsBatchHandler için zorunlu kategori özniteliği testleri.</summary>
public class CreateProductsBatchHandlerTests
{
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IVariantRepository> _variants = new();
    private readonly Mock<IAttributeRepository> _attributes = new();
    private readonly Mock<ISkuGeneratorService> _skuGenerator = new();

    public CreateProductsBatchHandlerTests()
    {
        HandlerTestSupport.SetupPassthroughPlans(_skuGenerator);
    }

    [Fact]
    public async Task ExecuteAsync_RequiredCategoryAttributeMissing_ReturnsValidationError()
    {
        var (category, attribute) = HandlerTestSupport.CategoryWithRequiredAttribute();
        _categories
            .Setup(c => c.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        _attributes
            .Setup(a => a.GetByIdAsync(attribute.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        var handler = CreateHandler();

        var result = await handler.ExecuteAsync(new CreateProductsBatchCommand(
            Guid.NewGuid(),
            [BuildItem(category.Id, attributeValues: null)]));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Validation);
        result.Error.Message.Should().Be($"Required attribute missing: {attribute.Name}");
        _products.Verify(p => p.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_RequiredCategoryAttributeProvided_Succeeds()
    {
        var (category, attribute) = HandlerTestSupport.CategoryWithRequiredAttribute();
        var attributeValue = attribute.Values.Single();
        _categories
            .Setup(c => c.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        _attributes
            .Setup(a => a.GetByIdAsync(attribute.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        var handler = CreateHandler();

        var result = await handler.ExecuteAsync(new CreateProductsBatchCommand(
            Guid.NewGuid(),
            [BuildItem(category.Id, [new AttributeValueInput(attribute.Id, attributeValue.Id)])]));

        result.IsSuccess.Should().BeTrue();
        result.Value.Products.Should().ContainSingle();
        _products.Verify(p => p.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static CreateProductsBatchItem BuildItem(
        Guid categoryId,
        IReadOnlyList<AttributeValueInput>? attributeValues) =>
        new(
            categoryId,
            "SKU-001",
            "Title",
            "draft",
            null,
            attributeValues,
            [],
            [HandlerTestSupport.ValidItem()]);

    private CreateProductsBatchHandler CreateHandler() =>
        new(
            new CreateProductsBatchCommandValidator(),
            _products.Object,
            _categories.Object,
            _variants.Object,
            _attributes.Object,
            _skuGenerator.Object,
            Mock.Of<IUnitOfWork>());
}

/// <summary>Ürün oluşturma handler testleri için ortak kurulum yardımcıları.</summary>
internal static class HandlerTestSupport
{
    /// <summary>Zorunlu bir öznitelik atanmış kategori ile tek değerli özniteliği oluşturur.</summary>
    internal static (Category Category, AttributeDefinition Attribute) CategoryWithRequiredAttribute()
    {
        var attribute = AttributeDefinition.Create("Material").Value;
        attribute.AddValue("Cotton");

        var category = Category.Create("Apparel", null, null).Value;
        category.AssignAttribute(attribute.Id, required: true, sortOrder: 0);

        return (category, attribute);
    }

    /// <summary>Geçerli tek kalem girdisi üretir.</summary>
    internal static CreateProductItemInput ValidItem() =>
        new(null, "8690000001", null, null, null, null, 10m, null, 5, null, null);

    /// <summary>SKU üreticisini, girdileri tek plana aynen aktaracak şekilde ayarlar.</summary>
    internal static void SetupPassthroughPlans(Mock<ISkuGeneratorService> skuGenerator) =>
        skuGenerator
            .Setup(s => s.BuildPlansAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>?>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<Variant>>(),
                It.IsAny<IReadOnlyList<ProductItemDraft>>(),
                It.IsAny<IReadOnlyList<ProductSplitOverride>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                string modelCode,
                IReadOnlyList<string>? codeInputs,
                string name,
                IReadOnlyList<Variant> variants,
                IReadOnlyList<ProductItemDraft> drafts,
                IReadOnlyList<ProductSplitOverride>? overrides,
                CancellationToken token) =>
                Result.Success<IReadOnlyList<ProductCreatePlan>>(
                    [new ProductCreatePlan(modelCode, name, variants, drafts)]));
}
