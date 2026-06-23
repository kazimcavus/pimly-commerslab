using Catalog.Domain.Attributes;
using FluentAssertions;
using DomainAttribute = Catalog.Domain.Attributes.Attribute;

namespace Catalog.Domain.UnitTests;

/// <summary>Attribute varlığı için birim testleri.</summary>
public class AttributeTests
{
    [Fact]
    public void Create_WithEmptyName_Fails()
    {
        var result = DomainAttribute.Create("  ");
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_WithName_Succeeds()
    {
        var result = DomainAttribute.Create("Yaka Tipi");
        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Value.Should().Be("YAKA_TIPI");
        result.Value.Name.Should().Be("Yaka Tipi");
    }

    [Fact]
    public void AddValue_DuplicateName_Fails()
    {
        var attribute = DomainAttribute.Create("Yaka Tipi").Value;

        attribute.AddValue("V Yaka").IsSuccess.Should().BeTrue();
        var duplicate = attribute.AddValue("v yaka");

        duplicate.IsFailure.Should().BeTrue();
        duplicate.Error.Code.Should().Be("conflict");
    }

    [Fact]
    public void RemoveValue_UnknownValue_Fails()
    {
        var attribute = DomainAttribute.Create("Yaka Tipi").Value;
        var result = attribute.RemoveValue(Guid.NewGuid());
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("not_found");
    }
}
