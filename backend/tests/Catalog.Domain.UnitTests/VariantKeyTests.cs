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
}
