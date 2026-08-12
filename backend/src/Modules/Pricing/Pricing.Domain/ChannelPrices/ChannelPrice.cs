using Pricing.Domain.ChannelPrices.Events;
using SharedKernel;

namespace Pricing.Domain.ChannelPrices;

/// <summary>
/// Bir satılabilir kalemin belirli bir pazaryerindeki (kanal) kararlaştırılmış fiyatı ve opsiyonel
/// karşılaştırma fiyatı. Kalem × marketplace başına tek kayıt tutulur; kaleme opak
/// <see cref="ProductItemId"/> ile referanslanır (bağlamlar arası yabancı anahtar kurulmaz).
/// Karar Pricing'e aittir; bu fiyatın pazaryerine gönderilmesi (yayın) Channels'ın işidir.
/// </summary>
/// <example>Trendyol'da 449.90 ₺, karşılaştırma 599.90 ₺.</example>
public sealed class ChannelPrice : AggregateRoot<Guid>
{
    private ChannelPrice()
    {
    }

    private ChannelPrice(
        Guid id,
        Guid productItemId,
        Marketplace marketplace,
        decimal amount,
        decimal? compareAtAmount,
        string currency,
        DateTimeOffset updatedAt)
        : base(id)
    {
        ProductItemId = productItemId;
        Marketplace = marketplace;
        Amount = amount;
        CompareAtAmount = compareAtAmount;
        Currency = currency;
        UpdatedAt = updatedAt;
    }

    /// <summary>Gets bağlı satılabilir kalemin kimliği.</summary>
    public Guid ProductItemId { get; private set; }

    /// <summary>Gets kanal fiyatının ait olduğu pazaryeri.</summary>
    public Marketplace Marketplace { get; private set; } = Marketplace.Trendyol;

    /// <summary>Gets kanal fiyat tutarı.</summary>
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

    /// <summary>Yeni kanal fiyatı oluşturur.</summary>
    public static Result<ChannelPrice> Create(
        Guid productItemId,
        Marketplace marketplace,
        decimal amount,
        decimal? compareAtAmount = null,
        string? currency = null)
    {
        if (productItemId == Guid.Empty)
        {
            return Result.Failure<ChannelPrice>(Error.Validation("Product item id is required."));
        }

        if (marketplace is null)
        {
            return Result.Failure<ChannelPrice>(Error.Validation("Marketplace is required."));
        }

        var amountResult = ValidateAmounts(amount, compareAtAmount);
        if (amountResult.IsFailure)
        {
            return Result.Failure<ChannelPrice>(amountResult.Error);
        }

        var channelPrice = new ChannelPrice(
            Guid.NewGuid(),
            productItemId,
            marketplace,
            amount,
            compareAtAmount,
            NormalizeCurrency(currency),
            DateTimeOffset.UtcNow);

        channelPrice.RaiseDomainEvent(new ChannelPriceChanged(productItemId, marketplace.Code));
        return Result.Success(channelPrice);
    }

    /// <summary>Kanal fiyatını, karşılaştırma fiyatını ve opsiyonel para birimini günceller.</summary>
    public Result Update(decimal amount, decimal? compareAtAmount = null, string? currency = null)
    {
        var amountResult = ValidateAmounts(amount, compareAtAmount);
        if (amountResult.IsFailure)
        {
            return amountResult;
        }

        var normalizedCurrency = NormalizeCurrency(currency);

        // Değerler aynıysa olay yayımlanmaz: gereksiz kanal senkronu tetiklenmesin.
        if (Amount == amount
            && CompareAtAmount == compareAtAmount
            && string.Equals(Currency, normalizedCurrency, StringComparison.Ordinal))
        {
            return Result.Success();
        }

        Amount = amount;
        CompareAtAmount = compareAtAmount;
        Currency = normalizedCurrency;
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new ChannelPriceChanged(ProductItemId, Marketplace.Code));
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
