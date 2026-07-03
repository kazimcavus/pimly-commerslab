using SharedKernel;

namespace Catalog.Domain.Products;

/// <summary>Ürün seviyesinde benzersiz model kodu değer nesnesi.</summary>
/// <example>GOMlek-001.</example>
public sealed class ModelCode : ValueObject
{
    /// <summary>Gets normalize edilmiş model kodu metni.</summary>
    public string Value { get; }

    private ModelCode(string value)
    {
        Value = value;
    }

    /// <summary>Kalıcı depodan okunan model kodu değerini yeniden oluşturur; doğrulama yapmaz.</summary>
    /// <param name="value">Depodan okunan model kodu.</param>
    public static ModelCode FromPersistence(string value) => new(value);

    /// <summary>Model kodu metnini doğrulayarak yeni değer nesnesi oluşturur.</summary>
    /// <param name="value">Oluşturulacak model kodu.</param>
    public static Result<ModelCode> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<ModelCode>(Error.Validation("Model code is required."));
        }

        return Result.Success(new ModelCode(value.Trim()));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
