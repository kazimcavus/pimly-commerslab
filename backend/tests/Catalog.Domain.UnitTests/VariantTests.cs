using Catalog.Domain.Variants;
using FluentAssertions;

namespace Catalog.Domain.UnitTests;

/// <summary>Variant aggregate kökü için birim testleri.</summary>
public class VariantTests
{
    [Fact]
    public void Create_WithEmptyName_Fails()
    {
        var result = Variant.Create("  ", SelectionStyle.List, 0);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_WithValidName_DerivesKeyFromName()
    {
        var result = Variant.Create("Color", SelectionStyle.Color, 0);

        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Value.Should().Be("COLOR");
    }

    [Fact]
    public void Create_WithExplicitKey_UsesProvidedKey()
    {
        var result = Variant.Create("Color", SelectionStyle.Color, 0, key: "custom_color");

        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Value.Should().Be("custom_color");
    }

    [Fact]
    public void AddValue_DerivesKeyFromLabelWhenNotProvided()
    {
        var variant = Variant.Create("Color", SelectionStyle.Color, 0).Value;

        var addResult = variant.AddValue("Red", "#ff0000", null, null, 0);

        addResult.IsSuccess.Should().BeTrue();
        addResult.Value.Key.Value.Should().Be("RED");
    }

    [Fact]
    public void AddValue_WithExplicitKey_UsesProvidedKey()
    {
        var variant = Variant.Create("Color", SelectionStyle.Color, 0).Value;

        var addResult = variant.AddValue("Red", "#ff0000", null, "R08", 0);

        addResult.IsSuccess.Should().BeTrue();
        addResult.Value.Key.Value.Should().Be("R08");
    }

    [Fact]
    public void AddValue_DuplicateLabel_Fails()
    {
        var variant = Variant.Create("Color", SelectionStyle.Color, 0).Value;

        variant.AddValue("Red", "#ff0000", null, null, 0).IsSuccess.Should().BeTrue();
        var duplicate = variant.AddValue("red", "#00ff00", null, null, 1);

        duplicate.IsFailure.Should().BeTrue();
        duplicate.Error.Code.Should().Be("conflict");
    }

    [Fact]
    public void RemoveValue_ExistingValue_Succeeds()
    {
        var variant = Variant.Create("Color", SelectionStyle.Color, 0).Value;
        var value = variant.AddValue("Red", "#ff0000", null, null, 0).Value;

        var result = variant.RemoveValue(value.Id);

        result.IsSuccess.Should().BeTrue();
        variant.Values.Should().BeEmpty();
    }
}
