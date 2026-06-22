using Catalog.Domain.Attributes.Events;
using SharedKernel;

namespace Catalog.Domain.Attributes;

/// <summary>
/// Sistemde tanımlanan ürünlere eklenecek özelliklerin kök varlığı.
/// Özellik adını ve seçilebilir değerlerini yönetir.
/// </summary>
/// <example>
/// "Yaka Tipi" adıyla oluşturulan özelliğin key değeri yaka_tipi olur;
/// "Bisiklet Yaka" ve "V Yaka" değerleri eklenir.
/// </example>
public sealed class Attribute : AggregateRoot<Guid>
{
    private readonly List<AttributeValue> _values = [];

    private Attribute()
    {
    }

    private Attribute(Guid id, AttributeKey key, string name)
        : base(id)
    {
        Key = key;
        Name = name;
    }

    /// <summary>Gets özelliği benzersiz tanımlayan anahtar; oluşturulurken adından türetilir.</summary>
    /// <example>yaka_tipi.</example>
    public AttributeKey Key { get; private set; } = null!;

    /// <summary>Gets özelliğin kullanıcıya gösterilen adı.</summary>
    /// <example>Yaka Tipi.</example>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets özelliğe ait seçilebilir değerler.</summary>
    public IReadOnlyCollection<AttributeValue> Values => _values.AsReadOnly();

    public static Result<Attribute> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Attribute>(Error.Validation("Attribute name is required."));
        }

        var trimmedName = name.Trim();
        var keyResult = AttributeKey.FromName(trimmedName);
        if (keyResult.IsFailure)
        {
            return Result.Failure<Attribute>(keyResult.Error);
        }

        var attribute = new Attribute(Guid.NewGuid(), keyResult.Value, trimmedName);

        attribute.RaiseDomainEvent(new AttributeCreated(attribute.Id, attribute.Key.Value));
        return Result.Success(attribute);
    }

    public Result Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("Attribute name is required."));
        }

        Name = name.Trim();
        return Result.Success();
    }

    public Result<AttributeValue> AddValue(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<AttributeValue>(Error.Validation("Attribute value name is required."));
        }

        var trimmedName = name.Trim();
        if (_values.Any(v => string.Equals(v.Name, trimmedName, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure<AttributeValue>(
                Error.Conflict("Attribute value name must be unique within the attribute."));
        }

        var value = new AttributeValue(Guid.NewGuid(), trimmedName);
        _values.Add(value);
        return Result.Success(value);
    }

    public Result UpdateValue(Guid valueId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("Attribute value name is required."));
        }

        var value = _values.FirstOrDefault(v => v.Id == valueId);
        if (value is null)
        {
            return Result.Failure(Error.NotFound("Attribute value not found."));
        }

        var trimmedName = name.Trim();
        if (_values.Any(v => v.Id != valueId &&
                             string.Equals(v.Name, trimmedName, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure(Error.Conflict("Attribute value name must be unique within the attribute."));
        }

        value.Update(trimmedName);
        return Result.Success();
    }

    public Result RemoveValue(Guid valueId)
    {
        var value = _values.FirstOrDefault(v => v.Id == valueId);
        if (value is null)
        {
            return Result.Failure(Error.NotFound("Attribute value not found."));
        }

        _values.Remove(value);
        return Result.Success();
    }

    internal void LoadValues(IEnumerable<AttributeValue> values)
    {
        _values.Clear();
        _values.AddRange(values);
    }
}
