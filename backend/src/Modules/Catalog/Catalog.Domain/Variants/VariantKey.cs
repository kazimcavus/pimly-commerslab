using System.Text;
using SharedKernel;

namespace Catalog.Domain.Variants;

/// <summary>
/// Varyant türünü benzersiz şekilde tanımlayan anahtar değer nesnesi; aggregate oluşturulurken adından türetilir.
/// </summary>
/// <example>renk.</example>
public sealed class VariantKey : ValueObject
{
    public const int MaxLength = 200;

    public string Value { get; }

    private VariantKey(string value)
    {
        Value = value;
    }

    public static VariantKey FromPersistence(string value) => new(value);

    internal static Result<VariantKey> FromName(string name)
    {
        var normalized = name.ToLowerInvariant()
            .Replace('ı', 'i')
            .Replace('ş', 's')
            .Replace('ğ', 'g')
            .Replace('ü', 'u')
            .Replace('ö', 'o')
            .Replace('ç', 'c');

        var builder = new StringBuilder(normalized.Length);
        var pendingSeparator = false;

        foreach (var ch in normalized)
        {
            if (char.IsAsciiLetterOrDigit(ch))
            {
                if (pendingSeparator && builder.Length > 0)
                {
                    builder.Append('_');
                }

                builder.Append(ch);
                pendingSeparator = false;
                continue;
            }

            if (!pendingSeparator && builder.Length > 0)
            {
                pendingSeparator = true;
            }
        }

        return Create(builder.ToString());
    }

    internal static Result<VariantKey> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<VariantKey>(Error.Validation("Variant key is required."));
        }

        var normalized = value.Trim();
        if (normalized.Length > MaxLength)
        {
            return Result.Failure<VariantKey>(Error.Validation($"Variant key must be at most {MaxLength} characters."));
        }

        return Result.Success(new VariantKey(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
