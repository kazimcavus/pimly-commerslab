using Catalog.Domain.Variants;
using FluentAssertions;
using SharedKernel;

namespace Catalog.Domain.UnitTests;

/// <summary>VariantKey türetimi için birim testleri (Variant aggregate üzerinden).</summary>
public class VariantKeyTests
{
    private static Result<Variant> CreateVariant(string name) =>
        Variant.Create(name, SelectionStyle.List, 0);

    [Fact]
    public void FromName_TurkishCharacters_NormalizesToAsciiKey()
    {
        var result = CreateVariant("Renk");
        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Value.Should().Be("RENK");
    }

    [Fact]
    public void FromName_SpecialCharacters_InsertsSeparators()
    {
        var result = CreateVariant("Size (EU)");
        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Value.Should().Be("SIZE_EU");
    }

    [Fact]
    public void FromName_OnlyNonAlphanumericCharacters_Fails()
    {
        var result = CreateVariant("---");
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Key");
    }

    [Fact]
    public void FromName_TooLongKey_Fails()
    {
        var result = CreateVariant(new string('a', VariantKey.MaxLength + 1));
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Key");
    }

    [Fact]
    public void FromName_MaxLengthKey_Succeeds()
    {
        var result = CreateVariant(new string('a', VariantKey.MaxLength));
        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Value.Should().HaveLength(VariantKey.MaxLength);
    }

    [Fact]
    public void FromName_DuplicateNormalizedKey_ProducesSameKey()
    {
        var first = CreateVariant("Renk");
        var second = CreateVariant("Renk  ");

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        first.Value.Key.Value.Should().Be(second.Value.Key.Value);
    }

    // Yalnızca ayraç (boşluk/noktalama) bakımından farklı etiketler aynı anahtarı üretir.
    [Theory]
    [InlineData("Krem-Bej", "Krem Bej")]
    [InlineData("80 x 200", "80  x  200")]
    [InlineData("80 x 200", "80 x 200 ")]
    public void TryPreview_LabelsDifferingOnlyBySeparators_ProduceSameKey(string a, string b)
    {
        VariantKey.TryPreview(a).Should().Be(VariantKey.TryPreview(b));
    }

    [Fact]
    public void TryPreview_MatchesPersistedValueKey()
    {
        // İçe aktarım tam da bu eşleştirmeyi yapar: kalıcı değerin anahtarı ile
        // yeni etiketin önizleme anahtarı çakışırsa mevcut değer yeniden kullanılır.
        var variant = CreateVariant("Boyut").Value;
        var added = variant.AddValue("80 x 200", null, null, null, 0);

        added.IsSuccess.Should().BeTrue();
        VariantKey.TryPreview("80  x  200").Should().Be(added.Value.Key.Value);
    }

    [Fact]
    public void TryPreview_InvalidName_ReturnsNull()
    {
        VariantKey.TryPreview("---").Should().BeNull();
    }

    [Fact]
    public void AddValue_DifferentLabelsSameKey_ConflictsOnKey()
    {
        // Düzeltilen hatanın kök nedeni: farklı etiketler aynı slug-anahtara indirgenince
        // ikinci ekleme anahtar çakışmasına düşer. Gateway artık önizleme anahtarıyla
        // eşleştirip mevcut değeri yeniden kullanarak bunu önler.
        var variant = CreateVariant("Boyut").Value;
        variant.AddValue("Krem-Bej", null, null, null, 0).IsSuccess.Should().BeTrue();

        var second = variant.AddValue("Krem Bej", null, null, null, 1);

        second.IsFailure.Should().BeTrue();
        second.Error.Message.Should().Contain("key must be unique");
    }
}
