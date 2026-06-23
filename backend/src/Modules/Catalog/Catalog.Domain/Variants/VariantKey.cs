using SharedKernel;

namespace Catalog.Domain.Variants;

/// <summary>
/// Varyant türünü benzersiz şekilde tanımlayan anahtar değer nesnesi; oluşturulurken adından türetilir.
/// </summary>
/// <example>RENK.</example>
public sealed class VariantKey : ValueObject
{
    public const int MaxLength = CatalogKeyGenerator.MaxLength;

    public string Value { get; }

    private VariantKey(string value)
    {
        Value = value;
    }

    public static VariantKey FromPersistence(string value) => new(value);

    internal static Result<VariantKey> FromOptional(string? key, string fallbackName) =>
        string.IsNullOrWhiteSpace(key)
            ? FromName(fallbackName)
            : Create(key);

    internal static Result<VariantKey> FromName(string name)
    {
        var generateResult = CatalogKeyGenerator.GenerateFromName(name);
        return generateResult.IsFailure
            ? Result.Failure<VariantKey>(generateResult.Error)
            : Result.Success(new VariantKey(generateResult.Value));
    }

    internal static Result<VariantKey> Create(string value)
    {
        var validateResult = CatalogKeyGenerator.ValidateExplicit(value);
        return validateResult.IsFailure
            ? Result.Failure<VariantKey>(validateResult.Error)
            : Result.Success(new VariantKey(validateResult.Value));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
