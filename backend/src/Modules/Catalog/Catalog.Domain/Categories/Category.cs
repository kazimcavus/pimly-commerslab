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

    /// <summary>Yeni kategori oluşturur ve <see cref="CategoryCreated"/> alan olayını yayımlar.</summary>
    /// <param name="name">Kategori adı.</param>
    /// <param name="code">Opsiyonel kategori kodu.</param>
    /// <param name="parentId">Üst kategori tanımlayıcısı; kök kategori için null.</param>
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

    /// <summary>Kategori adını ve opsiyonel kodunu günceller.</summary>
    /// <param name="name">Yeni kategori adı.</param>
    /// <param name="code">Yeni kategori kodu; boş bırakılırsa null olur.</param>
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

    /// <summary>Kategoriyi hiyerarşide başka bir üst kategoriye taşır.</summary>
    /// <param name="parentId">Yeni üst kategori; kök seviyeye almak için null.</param>
    /// <param name="descendantIds">Döngü oluşturmayı önlemek için bu kategorinin alt soy tanımlayıcıları.</param>
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

    /// <summary>Kategoriye yeni bir öznitelik ataması ekler.</summary>
    /// <param name="attributeId">Atanacak öznitelik tanımlayıcısı.</param>
    /// <param name="required">Öznitelik bu kategoride zorunlu mu.</param>
    /// <param name="sortOrder">Kategori içindeki görüntüleme sırası.</param>
    /// <param name="scope">Özniteliğin seçim seviyesi (model / slicer değeri / kalem).</param>
    public Result<CategoryAttributeAssignment> AssignAttribute(
        Guid attributeId,
        bool required,
        int sortOrder,
        AttributeScope scope = AttributeScope.Model)
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
            sortOrder,
            scope);

        _assignments.Add(assignment);
        return Result.Success(assignment);
    }

    /// <summary>Mevcut bir öznitelik atamasının kurallarını günceller.</summary>
    /// <param name="assignmentId">Güncellenecek atama tanımlayıcısı.</param>
    /// <param name="required">Öznitelik bu kategoride zorunlu mu.</param>
    /// <param name="sortOrder">Kategori içindeki görüntüleme sırası.</param>
    /// <param name="scope">Özniteliğin seçim seviyesi; null verilirse mevcut seviye korunur.</param>
    public Result UpdateAssignment(
        Guid assignmentId,
        bool required,
        int sortOrder,
        AttributeScope? scope = null)
    {
        var assignment = _assignments.FirstOrDefault(a => a.Id == assignmentId);
        if (assignment is null)
        {
            return Result.Failure(Error.NotFound("Category attribute assignment not found."));
        }

        assignment.Update(required, sortOrder, scope ?? assignment.Scope);
        return Result.Success();
    }

    /// <summary>Kategoriden bir öznitelik atamasını kaldırır.</summary>
    /// <param name="assignmentId">Kaldırılacak atama tanımlayıcısı.</param>
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

    /// <summary>Kalınan öznitelik atamalarını yükler; kalıcılık katmanı tarafından kullanılır.</summary>
    /// <param name="assignments">Kategoriye ait atama koleksiyonu.</param>
    internal void LoadAssignments(IEnumerable<CategoryAttributeAssignment> assignments)
    {
        _assignments.Clear();
        _assignments.AddRange(assignments);
    }
}
