using Catalog.Domain.Categories.Events;
using SharedKernel;

namespace Catalog.Domain.Categories;

/// <summary>
/// Hiyerarşik yapıda kategorileri ve bunlara atanan öznitelikleri yöneten kök varlık.
/// </summary>
public sealed class Category : AggregateRoot<Guid>
{
    private readonly List<CategoryAttributeAssignment> _assignments = [];

    private Category()
    {
    }

    private Category(Guid id, string name, string? code, Guid? parentId)
        : base(id)
    {
        Name = name;
        Code = code;
        ParentId = parentId;
    }

    /// <summary>Gets kategorinin görünen adı.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets kategorinin opsiyonel kodu.</summary>
    public string? Code { get; private set; }

    /// <summary>Gets üst kategorinin tanımlayıcısı; kök kategori için null.</summary>
    public Guid? ParentId { get; private set; }

    /// <summary>Gets kategoriye atanan öznitelik eşlemeleri.</summary>
    public IReadOnlyCollection<CategoryAttributeAssignment> Assignments => _assignments.AsReadOnly();

    public static Result<Category> Create(string name, string? code, Guid? parentId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Category>(Error.Validation("Category name is required."));
        }

        var category = new Category(
            Guid.NewGuid(),
            name.Trim(),
            string.IsNullOrWhiteSpace(code) ? null : code.Trim(),
            parentId);

        category.RaiseDomainEvent(new CategoryCreated(category.Id, category.Name));
        return Result.Success(category);
    }

    public Result Rename(string name, string? code)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("Category name is required."));
        }

        Name = name.Trim();
        Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        return Result.Success();
    }

    public Result MoveToParent(Guid? parentId, IReadOnlySet<Guid> descendantIds)
    {
        if (parentId == Id)
        {
            return Result.Failure(Error.Validation("Category cannot be its own parent."));
        }

        if (parentId.HasValue && descendantIds.Contains(parentId.Value))
        {
            return Result.Failure(Error.Validation("Category cannot be moved under its own descendant."));
        }

        ParentId = parentId;
        return Result.Success();
    }

    public Result<CategoryAttributeAssignment> AssignAttribute(
        Guid attributeId,
        bool required,
        bool marketplaceRequired,
        int sortOrder)
    {
        if (_assignments.Any(a => a.AttributeId == attributeId))
        {
            return Result.Failure<CategoryAttributeAssignment>(
                Error.Conflict("Attribute is already assigned to this category."));
        }

        var assignment = new CategoryAttributeAssignment(
            Guid.NewGuid(),
            attributeId,
            required,
            marketplaceRequired,
            sortOrder);

        _assignments.Add(assignment);
        return Result.Success(assignment);
    }

    public Result UpdateAssignment(
        Guid assignmentId,
        bool required,
        bool marketplaceRequired,
        int sortOrder)
    {
        var assignment = _assignments.FirstOrDefault(a => a.Id == assignmentId);
        if (assignment is null)
        {
            return Result.Failure(Error.NotFound("Category attribute assignment not found."));
        }

        assignment.Update(required, marketplaceRequired, sortOrder);
        return Result.Success();
    }

    public Result RemoveAssignment(Guid assignmentId)
    {
        var assignment = _assignments.FirstOrDefault(a => a.Id == assignmentId);
        if (assignment is null)
        {
            return Result.Failure(Error.NotFound("Category attribute assignment not found."));
        }

        _assignments.Remove(assignment);
        return Result.Success();
    }

    internal void LoadAssignments(IEnumerable<CategoryAttributeAssignment> assignments)
    {
        _assignments.Clear();
        _assignments.AddRange(assignments);
    }
}
