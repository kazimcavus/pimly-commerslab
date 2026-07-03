using System.Text.RegularExpressions;
using SharedKernel;

namespace Channels.Domain.Marketplaces;

/// <summary>Pazaryerini benzersiz tanımlayan anahtar değer nesnesi.</summary>
/// <example>trendyol.</example>
public sealed partial class MarketplaceKey : ValueObject
{
    public const int MaxLength = 100;

    [GeneratedRegex("^[a-z0-9][a-z0-9_-]{0,98}[a-z0-9]$|^[a-z0-9]$", RegexOptions.CultureInvariant)]
    private static partial Regex KeyPattern();

    public string Value { get; }

    private MarketplaceKey(string value)
    {
        Value = value;
    }

    public static MarketplaceKey FromPersistence(string value) => new(value);

    public static Result<MarketplaceKey> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<MarketplaceKey>(Error.Validation("Marketplace key is required."));
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > MaxLength || !KeyPattern().IsMatch(normalized))
        {
            return Result.Failure<MarketplaceKey>(
                Error.Validation("Marketplace key must be 1-100 lowercase letters, digits, hyphens or underscores."));
        }

        return Result.Success(new MarketplaceKey(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
