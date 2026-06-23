using SharedKernel;

namespace Catalog.Domain.Barcodes;

/// <summary>Üretilen tek bir barkod kaydı.</summary>
public sealed class BarcodeAllocation : Entity<Guid>
{
    private BarcodeAllocation()
    {
    }

    private BarcodeAllocation(Guid id, string barcode, DateTimeOffset allocatedAt)
        : base(id)
    {
        Barcode = barcode;
        AllocatedAt = allocatedAt;
    }

    /// <summary>Gets sayısal barkod değeri (string olarak saklanır).</summary>
    public string Barcode { get; private set; } = string.Empty;

    /// <summary>Gets barkodun üretildiği zaman.</summary>
    public DateTimeOffset AllocatedAt { get; private set; }

    public static Result<BarcodeAllocation> Create(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode) || !IsNumericBarcode(barcode))
        {
            return Result.Failure<BarcodeAllocation>(Error.Validation("Barcode must be a numeric value."));
        }

        return Result.Success(new BarcodeAllocation(
            Guid.NewGuid(),
            barcode.Trim(),
            DateTimeOffset.UtcNow));
    }

    public static bool IsNumericBarcode(string value) =>
        value.All(char.IsDigit) && value.Length > 0;

    public static string FormatNumeric(long value) =>
        value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
