using SharedKernel;

namespace Catalog.Domain.Products;

/// <summary>
/// Satılabilir kalemin pazaryeri (kanal) bazlı fiyat override'ı.
/// Kanal fiyatı yoksa kalemin temel fiyatı (<see cref="ProductItem.Price"/>) geçerlidir.
/// </summary>
/// <example>
/// Kalem temel fiyatı 400 ₺ iken trendyol kanalında 449.90 ₺, hepsiburada'da kayıt yok → 400 ₺.
/// </example>
public sealed class ProductItemChannelPrice : AggregateRoot<Guid>
{
    /// <summary>Pazaryeri anahtarının izin verilen en büyük uzunluğu.</summary>
    public const int MarketplaceKeyMaxLength = 50;

    private ProductItemChannelPrice()
    {
    }

    private ProductItemChannelPrice(
        Guid id,
        Guid productItemId,
        string marketplaceKey,
        decimal price,
        decimal? compareAtPrice,
        string currency,
        DateTimeOffset updatedAt)
        : base(id)
    {
        ProductItemId = productItemId;
        MarketplaceKey = marketplaceKey;
        Price = price;
        CompareAtPrice = compareAtPrice;
        Currency = currency;
        UpdatedAt = updatedAt;
    }

    /// <summary>Gets bağlı satılabilir kalemin kimliği.</summary>
    public Guid ProductItemId { get; private set; }

    /// <summary>Gets pazaryeri anahtarı (ör. "trendyol").</summary>
    public string MarketplaceKey { get; private set; } = string.Empty;

    /// <summary>Gets kanal satış fiyatı.</summary>
    /// <example>449.90.</example>
    public decimal Price { get; private set; }

    /// <summary>Gets kanal karşılaştırma / indirim öncesi fiyatı; opsiyonel.</summary>
    /// <example>599.90.</example>
    public decimal? CompareAtPrice { get; private set; }

    /// <summary>Gets fiyat para birimi (ISO 4217).</summary>
    /// <example>TRY.</example>
    public string Currency { get; private set; } = "TRY";

    /// <summary>Gets son güncelleme zamanı.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Yeni kanal fiyatı oluşturur.</summary>
    public static Result<ProductItemChannelPrice> Create(
        Guid productItemId,
        string marketplaceKey,
        decimal price,
        decimal? compareAtPrice,
        string? currency = null)
    {
        if (productItemId == Guid.Empty)
        {
            return Result.Failure<ProductItemChannelPrice>(Error.Validation("Product item id is required."));
        }

        var keyResult = NormalizeMarketplaceKey(marketplaceKey);
        if (keyResult.IsFailure)
        {
            return Result.Failure<ProductItemChannelPrice>(keyResult.Error);
        }

        var priceResult = ValidatePrices(price, compareAtPrice);
        if (priceResult.IsFailure)
        {
            return Result.Failure<ProductItemChannelPrice>(priceResult.Error);
        }

        return Result.Success(new ProductItemChannelPrice(
            Guid.NewGuid(),
            productItemId,
            keyResult.Value,
            price,
            compareAtPrice,
            NormalizeCurrency(currency),
            DateTimeOffset.UtcNow));
    }

    /// <summary>Kanal fiyat bilgilerini günceller.</summary>
    public Result UpdatePrice(decimal price, decimal? compareAtPrice, string? currency = null)
    {
        var priceResult = ValidatePrices(price, compareAtPrice);
        if (priceResult.IsFailure)
        {
            return priceResult;
        }

        Price = price;
        CompareAtPrice = compareAtPrice;
        Currency = NormalizeCurrency(currency);
        UpdatedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    private static Result<string> NormalizeMarketplaceKey(string marketplaceKey)
    {
        if (string.IsNullOrWhiteSpace(marketplaceKey))
        {
            return Result.Failure<string>(Error.Validation("Marketplace key is required."));
        }

        var normalized = marketplaceKey.Trim().ToLowerInvariant();
        if (normalized.Length > MarketplaceKeyMaxLength)
        {
            return Result.Failure<string>(Error.Validation(
                $"Marketplace key cannot exceed {MarketplaceKeyMaxLength} characters."));
        }

        return Result.Success(normalized);
    }

    private static Result ValidatePrices(decimal price, decimal? compareAtPrice)
    {
        if (price < 0)
        {
            return Result.Failure(Error.Validation("Channel price cannot be negative."));
        }

        if (compareAtPrice is < 0)
        {
            return Result.Failure(Error.Validation("Channel compare at price cannot be negative."));
        }

        return Result.Success();
    }

    private static string NormalizeCurrency(string? currency) =>
        string.IsNullOrWhiteSpace(currency) ? "TRY" : currency.Trim().ToUpperInvariant();
}
