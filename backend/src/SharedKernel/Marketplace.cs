namespace SharedKernel;

/// <summary>
/// Platform tarafından entegre edilen kapalı evren pazaryeri. Bağlamlar arası paylaşılan kanonik
/// kimlik (SharedKernel): hem Channels (yayın) hem Pricing (karar) aynı Marketplace'e referans verir.
/// </summary>
public sealed class Marketplace : ValueObject
{
    public const int MaxCodeLength = 10;

    public static readonly Marketplace Trendyol = new("TY", "Trendyol");

    private static readonly Marketplace[] All = [Trendyol];

    private Marketplace(string code, string name)
    {
        Code = code;
        Name = name;
    }

    /// <summary>Gets the marketplace code (e.g. TY, HB).</summary>
    public string Code { get; }

    /// <summary>Gets the marketplace display name.</summary>
    public string Name { get; }

    /// <summary>Gets all integrated marketplaces.</summary>
    public static IReadOnlyList<Marketplace> AllSupported => All;

    /// <summary>Ham kod string ile bilinen pazaryeri arar.</summary>
    public static Result<Marketplace> FromCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<Marketplace>(Error.Validation("Marketplace code is required."));
        }

        var normalized = code.Trim().ToUpperInvariant();
        var marketplace = All.FirstOrDefault(entry => entry.Code == normalized);
        return marketplace is null
            ? Result.Failure<Marketplace>(Error.NotFound("Marketplace not found."))
            : Result.Success(marketplace);
    }

    /// <summary>Persistence katmanından bilinen kod ile pazaryeri yükler.</summary>
    public static Marketplace FromPersistence(string code)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var marketplace = All.FirstOrDefault(entry => entry.Code == normalized);
        if (marketplace is null)
        {
            throw new InvalidOperationException($"Unknown marketplace code '{code}' in persistence.");
        }

        return marketplace;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
    }

    public override string ToString() => Code;
}
