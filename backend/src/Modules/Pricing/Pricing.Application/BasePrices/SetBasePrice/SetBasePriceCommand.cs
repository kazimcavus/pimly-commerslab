namespace Pricing.Application.BasePrices.SetBasePrice;

/// <summary>Kalemin temel fiyatını (ve opsiyonel karşılaştırma fiyatını) oluşturma / güncelleme komutu.</summary>
public sealed record SetBasePriceCommand(
    Guid ProductItemId,
    decimal Amount,
    decimal? CompareAtAmount = null,
    string? Currency = null);
