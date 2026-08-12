using Inventory.Domain.StockLevels.Events;
using SharedKernel;

namespace Inventory.Domain.StockLevels;

/// <summary>
/// Satılabilir kalemin stok miktarı. Kalem başına tek kayıt tutulur (tek örtük depo); kaleme opak
/// <see cref="ProductItemId"/> ile referanslanır (bağlamlar arası yabancı anahtar kurulmaz).
/// Çok-depo, ledger ve rezervasyon sonraki dilimlerde eklenir.
/// </summary>
/// <example>Kalem için 25 adet.</example>
public sealed class StockLevel : AggregateRoot<Guid>
{
    private StockLevel()
    {
    }

    private StockLevel(Guid id, Guid productItemId, int quantity, DateTimeOffset updatedAt)
        : base(id)
    {
        ProductItemId = productItemId;
        Quantity = quantity;
        UpdatedAt = updatedAt;
    }

    /// <summary>Gets bağlı satılabilir kalemin kimliği.</summary>
    public Guid ProductItemId { get; private set; }

    /// <summary>Gets stok miktarı.</summary>
    /// <example>25.</example>
    public int Quantity { get; private set; }

    /// <summary>Gets son güncelleme zamanı.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Yeni stok kaydı oluşturur.</summary>
    public static Result<StockLevel> Create(Guid productItemId, int quantity)
    {
        if (productItemId == Guid.Empty)
        {
            return Result.Failure<StockLevel>(Error.Validation("Product item id is required."));
        }

        var quantityResult = ValidateQuantity(quantity);
        if (quantityResult.IsFailure)
        {
            return Result.Failure<StockLevel>(quantityResult.Error);
        }

        var stockLevel = new StockLevel(Guid.NewGuid(), productItemId, quantity, DateTimeOffset.UtcNow);
        stockLevel.RaiseDomainEvent(new StockLevelChanged(productItemId));
        return Result.Success(stockLevel);
    }

    /// <summary>Stok miktarını günceller.</summary>
    public Result SetQuantity(int quantity)
    {
        var quantityResult = ValidateQuantity(quantity);
        if (quantityResult.IsFailure)
        {
            return quantityResult;
        }

        // Değer aynıysa olay yayımlanmaz: aksi halde her kaydetme gereksiz kanal senkronu tetikler.
        if (Quantity == quantity)
        {
            return Result.Success();
        }

        Quantity = quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new StockLevelChanged(ProductItemId));
        return Result.Success();
    }

    private static Result ValidateQuantity(int quantity) =>
        quantity < 0
            ? Result.Failure(Error.Validation("Stock quantity cannot be negative."))
            : Result.Success();
}
