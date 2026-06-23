using Catalog.Domain;
using FluentAssertions;

namespace Catalog.Domain.UnitTests;

/// <summary>CatalogKeyGenerator için birim testleri.</summary>
public class CatalogKeyGeneratorTests
{
    [Fact]
    public void GenerateFromName_ProducesUppercaseKey()
    {
        var result = CatalogKeyGenerator.GenerateFromName("Renk");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("RENK");
    }

    [Fact]
    public void GenerateFromName_AttributeAndVariant_UseSameRules()
    {
        var attributeResult = CatalogKeyGenerator.GenerateFromName("Yaka Tipi");
        var variantResult = CatalogKeyGenerator.GenerateFromName("Yaka Tipi");

        attributeResult.IsSuccess.Should().BeTrue();
        variantResult.IsSuccess.Should().BeTrue();
        attributeResult.Value.Should().Be(variantResult.Value);
        attributeResult.Value.Should().Be("YAKA_TIPI");
    }

    [Fact]
    public void ValidateExplicit_PreservesProvidedCasing()
    {
        var result = CatalogKeyGenerator.ValidateExplicit("  custom_color  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("custom_color");
    }
}
