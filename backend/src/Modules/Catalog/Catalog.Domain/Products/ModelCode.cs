using SharedKernel;

namespace Catalog.Domain.Products;

/// <summary>Ürün seviyesinde benzersiz model kodu değer nesnesi.</summary>
/// <example>GOMlek-001.</example>
public sealed class ModelCode : ValueObject
{
    public string Value { get; }

    private ModelCode(string value)
    {
        Value = value;
    }

    public static ModelCode FromPersistence(string value) => new(value);

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
