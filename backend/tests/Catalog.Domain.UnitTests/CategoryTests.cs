using Catalog.Domain.Categories;
using FluentAssertions;

namespace Catalog.Domain.UnitTests;

/// <summary>Category aggregate kökü için birim testleri.</summary>
public class CategoryTests
{
    [Fact]
    public void Create_WithEmptyName_Fails()
    {
        var result = Category.Create("  ", null, null);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
    }

    [Fact]
    public void MoveToParent_ToSelf_Fails()
    {
        var category = Category.Create("Root", null, null).Value;
        var result = category.MoveToParent(category.Id, new HashSet<Guid>());
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void MoveToParent_ToDescendant_Fails()
    {
        var root = Category.Create("Root", null, null).Value;
        var child = Category.Create("Child", null, root.Id).Value;
        var descendants = new HashSet<Guid> { child.Id };

        var result = root.MoveToParent(child.Id, descendants);
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AssignAttribute_DuplicateAttribute_Fails()
    {
        var category = Category.Create("Apparel", null, null).Value;
        var attributeId = Guid.NewGuid();

        category.AssignAttribute(attributeId, false, 0).IsSuccess.Should().BeTrue();
        var duplicate = category.AssignAttribute(attributeId, true, 1);

        duplicate.IsFailure.Should().BeTrue();
        duplicate.Error.Code.Should().Be("conflict");
    }

    [Fact]
    public void RemoveAssignment_UnknownAssignment_Fails()
    {
        var category = Category.Create("Apparel", null, null).Value;
        var result = category.RemoveAssignment(Guid.NewGuid());
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("not_found");
    }

    [Fact]
    public void MoveToParent_ValidParent_Succeeds()
    {
        var root = Category.Create("Root", null, null).Value;
        var child = Category.Create("Child", null, null).Value;

        var result = child.MoveToParent(root.Id, new HashSet<Guid>());

        result.IsSuccess.Should().BeTrue();
        child.ParentId.Should().Be(root.Id);
    }
}
