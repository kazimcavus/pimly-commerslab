using Channels.Application.Imports;
using Channels.Application.Imports.Planning;
using FluentAssertions;

namespace Channels.UnitTests;

/// <summary>
/// ProductImportPlanner için birim testleri: varyant/özellik sınıflandırması,
/// slicer ve Renk sezgiseli, eksen sınırı ve fiyat/stok eşlemesi.
/// </summary>
public class ProductImportPlannerTests
{
    private static readonly IReadOnlyList<ProductImportAttributeDef> GomlekDefs =
    [
        new("attr-renk", "Renk", Required: true, AllowCustom: false, IsVariant: true, IsSlicer: true),
        new("attr-beden", "Beden", Required: true, AllowCustom: false, IsVariant: true, IsSlicer: false),
        new("attr-kumas", "Kumaş", Required: true, AllowCustom: false, IsVariant: false, IsSlicer: false),
    ];

    private static MarketplaceProductNode Row(
        string barcode,
        string mainId = "GOMLEK-001",
        string renk = "Mavi",
        string? renkId = "val-mavi",
        string beden = "S",
        string? bedenId = "val-s",
        decimal listPrice = 599.90m,
        decimal salePrice = 449.90m,
        int quantity = 10,
        string categoryId = "221",
        string? stockCode = null,
        string title = "Klasik Gömlek") =>
        new(
            barcode,
            title,
            mainId,
            "Pimly",
            stockCode ?? $"STK-{barcode}",
            quantity,
            listPrice,
            salePrice,
            "TRY",
            categoryId,
            "Gömlek",
            "Açıklama",
            Approved: true,
            ImageUrls: [],
            Attributes:
            [
                new MarketplaceProductAttributeNode("attr-renk", "Renk", renkId, renk, null),
                new MarketplaceProductAttributeNode("attr-beden", "Beden", bedenId, beden, null),
                new MarketplaceProductAttributeNode("attr-kumas", "Kumaş", "val-pamuk", "Pamuk", null),
            ]);

    private static IReadOnlyDictionary<string, IReadOnlyList<ProductImportAttributeDef>> Defs() =>
        new Dictionary<string, IReadOnlyList<ProductImportAttributeDef>> { ["221"] = GomlekDefs };

    [Fact]
    public void BuildPlan_GroupsByProductMainId()
    {
        var plan = ProductImportPlanner.BuildPlan(
            [Row("1"), Row("2", beden: "M", bedenId: "val-m"), Row("3", mainId: "GOMLEK-002")],
            Defs());

        plan.Groups.Should().HaveCount(2);
        plan.Groups.Select(g => g.ProductMainId).Should().BeEquivalentTo(["GOMLEK-001", "GOMLEK-002"]);
        plan.Groups.First(g => g.ProductMainId == "GOMLEK-001").Items.Should().HaveCount(2);
    }

    [Fact]
    public void BuildPlan_ClassifiesVariantsAndAttributes()
    {
        var plan = ProductImportPlanner.BuildPlan([Row("1")], Defs());

        var group = plan.Groups.Single();
        group.VariantAxes.Select(a => a.Name).Should().BeEquivalentTo(["Renk", "Beden"]);
        group.AttributeValues.Should().ContainSingle(a => a.AttributeName == "Kumaş" && a.ValueName == "Pamuk");
    }

    [Fact]
    public void BuildPlan_ColorAxisIsSlicerAndColorStyle()
    {
        var plan = ProductImportPlanner.BuildPlan([Row("1")], Defs());

        var renk = plan.Groups.Single().VariantAxes.Single(a => a.Name == "Renk");
        renk.IsColor.Should().BeTrue();
        renk.Slicer.Should().BeTrue();

        var beden = plan.Groups.Single().VariantAxes.Single(a => a.Name == "Beden");
        beden.Slicer.Should().BeFalse();
    }

    [Fact]
    public void BuildPlan_ColorNameHeuristic_MakesRenkSlicerEvenWithoutFlag()
    {
        // Kategori tanımında IsSlicer yok ama ad "Renk" → varsayılan slicer davranışı.
        var defs = new Dictionary<string, IReadOnlyList<ProductImportAttributeDef>>
        {
            ["221"] =
            [
                new("attr-renk", "Renk", true, false, IsVariant: true, IsSlicer: false),
                new("attr-beden", "Beden", true, false, IsVariant: true, IsSlicer: false),
            ],
        };

        var plan = ProductImportPlanner.BuildPlan([Row("1")], defs);

        plan.Groups.Single().VariantAxes.Single(a => a.Name == "Renk").Slicer.Should().BeTrue();
    }

    [Fact]
    public void BuildPlan_ColorBecomesVariantAxis_EvenWhenNotFlaggedVariant()
    {
        // Trendyol kategorisi rengi "variant" işaretlemese bile (IsVariant=false),
        // renk varsayılan olarak varyant ekseni + slicer olmalı; özelliğe düşmemeli.
        var defs = new Dictionary<string, IReadOnlyList<ProductImportAttributeDef>>
        {
            ["221"] =
            [
                new("attr-renk", "Renk", true, false, IsVariant: false, IsSlicer: false),
                new("attr-beden", "Beden", true, false, IsVariant: true, IsSlicer: false),
                new("attr-kumas", "Kumaş", true, false, IsVariant: false, IsSlicer: false),
            ],
        };

        var group = ProductImportPlanner.BuildPlan([Row("1")], defs).Groups.Single();

        var renk = group.VariantAxes.Single(a => a.Name == "Renk");
        renk.IsColor.Should().BeTrue();
        renk.Slicer.Should().BeTrue();
        group.AttributeValues.Should().NotContain(a => a.AttributeName == "Renk");
    }

    [Fact]
    public void BuildPlan_OnlyOneSlicerSurvives()
    {
        var defs = new Dictionary<string, IReadOnlyList<ProductImportAttributeDef>>
        {
            ["221"] =
            [
                new("attr-renk", "Renk", true, false, IsVariant: true, IsSlicer: true),
                new("attr-beden", "Beden", true, false, IsVariant: true, IsSlicer: true),
            ],
        };

        var plan = ProductImportPlanner.BuildPlan([Row("1")], defs);

        var group = plan.Groups.Single();
        group.VariantAxes.Count(a => a.Slicer).Should().Be(1);
        group.Warnings.Should().Contain(w => w.Contains("slicer", StringComparison.OrdinalIgnoreCase) || w.Contains("tek slicer"));
    }

    [Fact]
    public void BuildPlan_MoreThanThreeAxes_DemotesExtrasToItemAttributes()
    {
        var defs = new Dictionary<string, IReadOnlyList<ProductImportAttributeDef>>
        {
            ["221"] =
            [
                new("a1", "Renk", true, false, true, true),
                new("a2", "Beden", true, false, true, false),
                new("a3", "Numara", true, false, true, false),
                new("a4", "Kalıp", true, false, true, false),
            ],
        };

        var row = MakeRow(
            "1",
            "MAIN-1",
            "221",
            [
                new MarketplaceProductAttributeNode("a1", "Renk", "v1", "Mavi", null),
                new MarketplaceProductAttributeNode("a2", "Beden", "v2", "S", null),
                new MarketplaceProductAttributeNode("a3", "Numara", "v3", "42", null),
                new MarketplaceProductAttributeNode("a4", "Kalıp", "v4", "Slim", null),
            ]);

        var plan = ProductImportPlanner.BuildPlan([row], defs);

        var group = plan.Groups.Single();
        group.VariantAxes.Should().HaveCount(3);

        // Eksen sıralaması: slicer > renk > ada göre — alfabetik son kalan "Numara" indirgenir.
        group.Items.Single().ItemAttributeValues.Should().ContainSingle(a => a.AttributeName == "Numara");
        group.Warnings.Should().Contain(w => w.Contains("Numara"));
    }

    [Fact]
    public void BuildPlan_CompareAtOnlyWhenListPriceHigher()
    {
        var plan = ProductImportPlanner.BuildPlan(
            [Row("1", listPrice: 599.90m, salePrice: 449.90m), Row("2", beden: "M", bedenId: "val-m", listPrice: 449.90m, salePrice: 449.90m)],
            Defs());

        var items = plan.Groups.Single().Items;
        items.Single(i => i.Barcode == "1").CompareAtPrice.Should().Be(599.90m);
        items.Single(i => i.Barcode == "2").CompareAtPrice.Should().BeNull();
        items.Single(i => i.Barcode == "1").Price.Should().Be(449.90m);
    }

    [Fact]
    public void BuildPlan_DuplicateBarcode_KeepsFirstWithWarning()
    {
        var plan = ProductImportPlanner.BuildPlan([Row("1"), Row("1", beden: "M", bedenId: "val-m")], Defs());

        var group = plan.Groups.Single();
        group.Items.Should().HaveCount(1);
        group.Warnings.Should().Contain(w => w.Contains("Yinelenen barkod"));
    }

    [Fact]
    public void BuildPlan_MissingAxisValue_SkipsRow()
    {
        var broken = MakeRow(
            "9",
            "GOMLEK-001",
            "221",
            [new MarketplaceProductAttributeNode("attr-kumas", "Kumaş", "val-pamuk", "Pamuk", null)]);

        var plan = ProductImportPlanner.BuildPlan([Row("1"), broken], Defs());

        var group = plan.Groups.Single();
        group.Items.Should().HaveCount(1);
        group.Items.Single().Barcode.Should().Be("1");
        group.Warnings.Should().Contain(w => w.Contains('9', StringComparison.Ordinal));
    }

    [Fact]
    public void BuildPlan_EmptyCategory_FailsGroup()
    {
        var row = MakeRow("1", "MAIN-1", string.Empty, []);

        var plan = ProductImportPlanner.BuildPlan([row], Defs());

        plan.Groups.Single().Error.Should().NotBeNull();
    }

    [Fact]
    public void BuildPlan_CustomAttributeValue_UsedWhenValueMissing()
    {
        var row = MakeRow(
            "1",
            "MAIN-1",
            "221",
            [new MarketplaceProductAttributeNode("attr-kumas", "Kumaş", null, null, "Keten")]);

        var plan = ProductImportPlanner.BuildPlan([row], Defs());

        plan.Groups.Single().AttributeValues.Should()
            .ContainSingle(a => a.AttributeName == "Kumaş" && a.ValueName == "Keten" && a.ExternalValueId == null);
    }

    [Fact]
    public void BuildPlan_NegativeStock_ClampedToZero()
    {
        var plan = ProductImportPlanner.BuildPlan([Row("1", quantity: -3)], Defs());

        plan.Groups.Single().Items.Single().Stock.Should().Be(0);
    }

    [Fact]
    public void BuildPlan_ColorLevelStockCode_BecomesSplitCodeAndClearsItemSkus()
    {
        // Trendyol'da stok kodu çoğu zaman renk düzeyindedir: aynı rengin tüm bedenleri
        // aynı kodu taşır. Kod split'e (renk ürününün model kodu) taşınır, kalem SKU'ları boşalır.
        var plan = ProductImportPlanner.BuildPlan(
            [
                Row("1", renk: "Vizon", renkId: "val-vizon", beden: "S", stockCode: "25CSM02817NR03", title: "Vizon Klasik Halı"),
                Row("2", renk: "Vizon", renkId: "val-vizon", beden: "M", bedenId: "val-m", stockCode: "25CSM02817NR03", title: "Vizon Klasik Halı"),
                Row("3", renk: "Bej", renkId: "val-bej", beden: "S", stockCode: "25CSM02817CR03", title: "Bej Cashmira Halı"),
            ],
            Defs());

        var group = plan.Groups.Single();
        group.SplitOverrides.Should().HaveCount(2);

        var vizon = group.SplitOverrides.Single(s => s.ValueName == "Vizon");
        vizon.StockCode.Should().Be("25CSM02817NR03");
        vizon.Title.Should().Be("Vizon Klasik Halı");

        var bej = group.SplitOverrides.Single(s => s.ValueName == "Bej");
        bej.StockCode.Should().Be("25CSM02817CR03");
        bej.Title.Should().Be("Bej Cashmira Halı");

        group.Items.Should().OnlyContain(item => item.Sku == null);
    }

    [Fact]
    public void BuildPlan_ItemLevelStockCodes_KeepItemSkus_AndSplitCodeStaysEmpty()
    {
        // Renk içinde kalem başına farklı stok kodları → kod kalem düzeyindedir;
        // split koduna taşınmaz, SKU'lar eski kuralla kalemlere yazılır.
        var plan = ProductImportPlanner.BuildPlan(
            [
                Row("1", renk: "Vizon", renkId: "val-vizon", beden: "S"),
                Row("2", renk: "Vizon", renkId: "val-vizon", beden: "M", bedenId: "val-m"),
            ],
            Defs());

        var group = plan.Groups.Single();
        group.SplitOverrides.Single(s => s.ValueName == "Vizon").StockCode.Should().BeNull();
        group.Items.Select(i => i.Sku).Should().BeEquivalentTo(["STK-1", "STK-2"]);
    }

    [Fact]
    public void BuildPlan_SameStockCodeAcrossColors_NotUsedAsSplitCode()
    {
        // Aynı kod iki renkte görülüyorsa güvenilmezdir; hiçbir renge verilmez.
        var plan = ProductImportPlanner.BuildPlan(
            [
                Row("1", renk: "Vizon", renkId: "val-vizon", stockCode: "SHARED"),
                Row("2", renk: "Bej", renkId: "val-bej", stockCode: "SHARED"),
            ],
            Defs());

        var group = plan.Groups.Single();
        group.SplitOverrides.Should().OnlyContain(s => s.StockCode == null);
    }

    private static MarketplaceProductNode MakeRow(
        string barcode,
        string mainId,
        string categoryId,
        IReadOnlyList<MarketplaceProductAttributeNode> attributes) =>
        new(
            barcode,
            "Ürün",
            mainId,
            Brand: null,
            StockCode: null,
            Quantity: 1,
            ListPrice: 100m,
            SalePrice: 100m,
            CurrencyType: "TRY",
            ExternalCategoryId: categoryId,
            CategoryName: null,
            Description: null,
            Approved: true,
            ImageUrls: [],
            Attributes: attributes);
}
