using SharedKernel;

namespace Pricing.Domain.BasePrices;

/// <summary>
/// Satılabilir kalemin temel (site/genel) fiyatı ve opsiyonel karşılaştırma (üstü çizili) fiyatı.
/// Kalem başına tek kayıt tutulur; kaleme opak <see cref="ProductItemId"/> ile referanslanır
/// (bağlamlar arası yabancı anahtar kurulmaz). Fiyat tanımı bazlı tutarlardan ayrı yaşar.
/// </summary>
/// <example>Temel 449.90 ₺, karşılaştırma 599.90 ₺.</example>
public sealed class BasePrice : AggregateRoot<Guid>
{
    private BasePrice()
    {
    }

    private BasePrice(
        Guid id,
        Guid productItemId,
        decimal amount,
        decimal? compareAtAmount,
        string currency,
        DateTimeOffset updatedAt)
        : base(id)
    {
        ProductItemId = productItemId;
        Amount = amount;
        CompareAtAmount = compareAtAmount;
        Currency = currency;
        UpdatedAt = updatedAt;
    }

    /// <summary>Gets bağlı satılabilir kalemin kimliği.</summary>
    public Guid ProductItemId { get; private set; }

    /// <summary>Gets temel fiyat tutarı.</summary>
    /// <example>449.90.</example>
    public decimal Amount { get; private set; }

    /// <summary>Gets opsiyonel karşılaştırma (üstü çizili) fiyat tutarı.</summary>
    /// <example>599.90.</example>
    public decimal? CompareAtAmount { get; private set; }

    /// <summary>Gets fiyat para birimi (ISO 4217).</summary>
    /// <example>TRY.</example>
    public string Currency { get; private set; } = "TRY";

    /// <summary>Gets son güncelleme zamanı.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Yeni temel fiyat oluşturur.</summary>
    public static Result<BasePrice> Create(
        Guid productItemId,
        decimal amount,
        decimal? compareAtAmount = null,
        string? currency = null)
    {
        if (productItemId == Guid.Empty)
        {
            return Result.Failure<BasePrice>(Error.Validation("Product item id is required."));
        }

        var amountResult = ValidateAmounts(amount, compareAtAmount);
        if (amountResult.IsFailure)
        {
            return Result.Failure<BasePrice>(amountResult.Error);
        }

        return Result.Success(new BasePrice(
            Guid.NewGuid(),
            productItemId,
            amount,
            compareAtAmount,
            NormalizeCurrency(currency),
            DateTimeOffset.UtcNow));
    }

    /// <summary>Temel fiyatı, karşılaştırma fiyatını ve opsiyonel para birimini günceller.</summary>
    public Result Update(decimal amount, decimal? compareAtAmount = null, string? currency = null)
    {
        var amountResult = ValidateAmounts(amount, compareAtAmount);
        if (amountResult.IsFailure)
        {
            return amountResult;
        }

        Amount = amount;
        CompareAtAmount = compareAtAmount;
        Currency = NormalizeCurrency(currency);
        UpdatedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    private static Result ValidateAmounts(decimal amount, decimal? compareAtAmount)
    {
        if (amount < 0)
        {
            return Result.Failure(Error.Validation("Price amount cannot be negative."));
        }

        if (compareAtAmount is < 0)
        {
            return Result.Failure(Error.Validation("Compare-at amount cannot be negative."));
        }

        return Result.Success();
    }

    private static string NormalizeCurrency(string? currency) =>
        string.IsNullOrWhiteSpace(currency) ? "TRY" : currency.Trim().ToUpperInvariant();
}
