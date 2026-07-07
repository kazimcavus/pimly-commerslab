using SharedKernel;

namespace Catalog.Domain.Products;

/// <summary>
/// Satılabilir kalemin bir fiyat tanımındaki tutarı. Kalem × fiyat tanımı başına tek kayıt tutulur;
/// kalemin temel fiyatı (<see cref="ProductItem.Price"/>) genel/site fiyatı olarak ayrıca yaşar.
/// </summary>
/// <example>
/// "TY Satış" tanımında 449.90 ₺, "TY Karşılaştırma" tanımında 599.90 ₺.
/// </example>
public sealed class ProductItemPrice : AggregateRoot<Guid>
{
    private ProductItemPrice()
    {
    }

    private ProductItemPrice(
        Guid id,
        Guid productItemId,
        Guid priceDefinitionId,
        decimal amount,
        string currency,
        DateTimeOffset updatedAt)
        : base(id)
    {
        ProductItemId = productItemId;
        PriceDefinitionId = priceDefinitionId;
        Amount = amount;
        Currency = currency;
        UpdatedAt = updatedAt;
    }

    /// <summary>Gets bağlı satılabilir kalemin kimliği.</summary>
    public Guid ProductItemId { get; private set; }

    /// <summary>Gets bağlı fiyat tanımının kimliği.</summary>
    public Guid PriceDefinitionId { get; private set; }

    /// <summary>Gets fiyat tutarı.</summary>
    /// <example>449.90.</example>
    public decimal Amount { get; private set; }

    /// <summary>Gets fiyat para birimi (ISO 4217).</summary>
    /// <example>TRY.</example>
    public string Currency { get; private set; } = "TRY";

    /// <summary>Gets son güncelleme zamanı.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Yeni kalem fiyatı oluşturur.</summary>
    public static Result<ProductItemPrice> Create(
        Guid productItemId,
        Guid priceDefinitionId,
        decimal amount,
        string? currency = null)
    {
        if (productItemId == Guid.Empty)
        {
            return Result.Failure<ProductItemPrice>(Error.Validation("Product item id is required."));
        }

        if (priceDefinitionId == Guid.Empty)
        {
            return Result.Failure<ProductItemPrice>(Error.Validation("Price definition id is required."));
        }

        var amountResult = ValidateAmount(amount);
        if (amountResult.IsFailure)
        {
            return Result.Failure<ProductItemPrice>(amountResult.Error);
        }

        return Result.Success(new ProductItemPrice(
            Guid.NewGuid(),
            productItemId,
            priceDefinitionId,
            amount,
            NormalizeCurrency(currency),
            DateTimeOffset.UtcNow));
    }

    /// <summary>Fiyat tutarını ve opsiyonel para birimini günceller.</summary>
    public Result UpdateAmount(decimal amount, string? currency = null)
    {
        var amountResult = ValidateAmount(amount);
        if (amountResult.IsFailure)
        {
            return amountResult;
        }

        Amount = amount;
        Currency = NormalizeCurrency(currency);
        UpdatedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    private static Result ValidateAmount(decimal amount)
    {
        if (amount < 0)
        {
            return Result.Failure(Error.Validation("Price amount cannot be negative."));
        }

        return Result.Success();
    }

    private static string NormalizeCurrency(string? currency) =>
        string.IsNullOrWhiteSpace(currency) ? "TRY" : currency.Trim().ToUpperInvariant();
}
