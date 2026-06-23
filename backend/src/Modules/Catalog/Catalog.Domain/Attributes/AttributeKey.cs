using SharedKernel;

namespace Catalog.Domain.Attributes;

/// <summary>
/// Özelliği benzersiz şekilde tanımlayan anahtar değer nesnesi; aggregate oluşturulurken adından türetilir.
/// </summary>
/// <example>YAKA_TIPI.</example>
public sealed class AttributeKey : ValueObject
{
    public const int MaxLength = CatalogKeyGenerator.MaxLength;

    public string Value { get; }

    private AttributeKey(string value)
    {
        Value = value;
    }

    public static AttributeKey FromPersistence(string value) => new(value);

    internal static Result<AttributeKey> FromName(string name)
    {
        var generateResult = CatalogKeyGenerator.GenerateFromName(name);
        return generateResult.IsFailure
            ? Result.Failure<AttributeKey>(generateResult.Error)
            : Result.Success(new AttributeKey(generateResult.Value));
    }

    internal static Result<AttributeKey> Create(string value)
    {
        var validateResult = CatalogKeyGenerator.ValidateExplicit(value);
        return validateResult.IsFailure
            ? Result.Failure<AttributeKey>(validateResult.Error)
            : Result.Success(new AttributeKey(validateResult.Value));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
