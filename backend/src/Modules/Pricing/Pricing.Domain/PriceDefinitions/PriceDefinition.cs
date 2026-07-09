using SharedKernel;

namespace Pricing.Domain.PriceDefinitions;

/// <summary>
/// Kullanıcı tanımlı fiyat alanını (fiyat tanımı) yöneten kök varlık.
/// Kalem başına her tanım için tek tutar tutulur (<see cref="ItemPrices.ProductItemPrice"/>).
/// </summary>
/// <example>
/// "TY Satış", "TY Karşılaştırma", "Kendi site indirimli".
/// </example>
public sealed class PriceDefinition : AggregateRoot<Guid>
{
    private PriceDefinition()
    {
    }

    private PriceDefinition(Guid id, string name, string? code)
        : base(id)
    {
        Name = name;
        Code = code;
    }

    /// <summary>Gets fiyat tanımının görünen adı.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets fiyat tanımının opsiyonel makine kodu (ör. "ty_sale").</summary>
    public string? Code { get; private set; }

    /// <summary>Yeni fiyat tanımı oluşturur.</summary>
    /// <param name="name">Fiyat tanımı adı.</param>
    /// <param name="code">Opsiyonel fiyat tanımı kodu.</param>
    public static Result<PriceDefinition> Create(string name, string? code)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<PriceDefinition>(Error.Validation("Price definition name is required."));
        }

        var definition = new PriceDefinition(
            Guid.NewGuid(),
            name.Trim(),
            string.IsNullOrWhiteSpace(code) ? null : code.Trim());

        return Result.Success(definition);
    }

    /// <summary>Fiyat tanımının adını ve opsiyonel kodunu günceller.</summary>
    /// <param name="name">Yeni fiyat tanımı adı.</param>
    /// <param name="code">Yeni fiyat tanımı kodu; boş bırakılırsa null olur.</param>
    public Result Rename(string name, string? code)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("Price definition name is required."));
        }

        Name = name.Trim();
        Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        return Result.Success();
    }
}
