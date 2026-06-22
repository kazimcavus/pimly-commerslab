using Catalog.Domain.Attributes;
using FluentAssertions;
using SharedKernel;

namespace Catalog.Domain.UnitTests;

/// <summary>AttributeKey türetimi için birim testleri (Attribute aggregate üzerinden).</summary>
public class AttributeKeyTests
{
    private static Result<Catalog.Domain.Attributes.Attribute> CreateAttribute(string name) =>
        Catalog.Domain.Attributes.Attribute.Create(name);

    [Fact]
    public void FromName_TurkishCharacters_NormalizesToAsciiKey()
    {
        var result = CreateAttribute("Yaka Tipi");
        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Value.Should().Be("yaka_tipi");
    }

    [Fact]
    public void FromName_SpecialCharacters_InsertsSeparators()
    {
        var result = CreateAttribute("Size (EU)");
        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Value.Should().Be("size_eu");
    }

    [Fact]
    public void FromName_OnlyNonAlphanumericCharacters_Fails()
    {
        var result = CreateAttribute("---");
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("key");
    }

    [Fact]
    public void FromName_TooLongKey_Fails()
    {
        var result = CreateAttribute(new string('a', AttributeKey.MaxLength + 1));
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("key");
    }

    [Fact]
    public void FromName_MaxLengthKey_Succeeds()
    {
        var result = CreateAttribute(new string('a', AttributeKey.MaxLength));
        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Value.Should().HaveLength(AttributeKey.MaxLength);
    }

    [Fact]
    public void FromName_DuplicateNormalizedKey_FailsOnSecondAttribute()
    {
        var first = CreateAttribute("Yaka Tipi");
        var second = CreateAttribute("Yaka  Tipi");

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        first.Value.Key.Value.Should().Be(second.Value.Key.Value);
    }
}
