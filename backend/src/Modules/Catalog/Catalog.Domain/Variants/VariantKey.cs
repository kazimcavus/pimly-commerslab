using SharedKernel;

namespace Catalog.Domain.Variants;

/// <summary>
/// Varyant türünü benzersiz şekilde tanımlayan anahtar değer nesnesi; oluşturulurken adından türetilir.
/// </summary>
/// <example>RENK.</example>
public sealed class VariantKey : ValueObject
{
    /// <summary>Gets anahtarın izin verilen maksimum uzunluğu.</summary>
    public const int MaxLength = CatalogKeyGenerator.MaxLength;

    /// <summary>Gets normalize edilmiş varyant anahtarı metni.</summary>
    public string Value { get; }

    private VariantKey(string value)
    {
        Value = value;
    }

    /// <summary>Kalıcı depodan okunan anahtar değerini yeniden oluşturur; doğrulama yapmaz.</summary>
    /// <param name="value">Depodan okunan anahtar.</param>
    public static VariantKey FromPersistence(string value) => new(value);

    /// <summary>Açık anahtar verilmişse doğrular; yoksa yedek adından türetir.</summary>
    /// <param name="key">Açık anahtar; opsiyonel.</param>
    /// <param name="fallbackName">Anahtar boşsa türetim için kullanılacak ad.</param>
    internal static Result<VariantKey> FromOptional(string? key, string fallbackName) =>
        string.IsNullOrWhiteSpace(key)
            ? FromName(fallbackName)
            : Create(key);

    /// <summary>Ad metninden slug anahtar üretir.</summary>
    /// <param name="name">Türetim kaynağı ad.</param>
    internal static Result<VariantKey> FromName(string name)
    {
        var generateResult = CatalogKeyGenerator.GenerateFromName(name);
        return generateResult.IsFailure
            ? Result.Failure<VariantKey>(generateResult.Error)
            : Result.Success(new VariantKey(generateResult.Value));
    }

    /// <summary>Açık anahtar metnini doğrulayarak yeni değer nesnesi oluşturur.</summary>
    /// <param name="value">Oluşturulacak anahtar.</param>
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
