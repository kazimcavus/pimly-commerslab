using SharedKernel;

namespace Catalog.Domain.Barcodes;

/// <summary>Sayısal barkod serisinin sıradaki değerini ve tahsis modunu tutan tek satırlık ayar.</summary>
public sealed class BarcodeSequence : Entity<int>
{
    public const int SingletonId = 1;

    private BarcodeSequence()
    {
    }

    private BarcodeSequence(int id, long nextValue, bool clientAllocationRequired)
        : base(id)
    {
        NextValue = nextValue;
        ClientAllocationRequired = clientAllocationRequired;
    }

    /// <summary>Gets bir sonraki verilecek sayısal barkod değeri.</summary>
    public long NextValue { get; private set; }

    /// <summary>Gets a value indicating whether istemci ürün oluşturmadan önce allocate endpoint'ini çağırmalıdır.</summary>
    public bool ClientAllocationRequired { get; private set; }

    /// <summary>NextValue 1 ve istemci tahsisi kapalı olacak şekilde başlangıç serisini oluşturur.</summary>
    public static BarcodeSequence CreateInitial() =>
        new(SingletonId, 1, false);

    /// <summary>Belirtilen adet kadar ardışık barkod aralığını ayırır ve <see cref="NextValue"/>'yu ilerletir.</summary>
    /// <param name="count">Ayrılacak barkod sayısı; en az 1 olmalıdır.</param>
    /// <returns>Başlangıç değeri ile güncellenmiş sıradaki değeri içeren sonuç.</returns>
    public Result<(long StartValue, long NewNextValue)> ReserveNext(int count)
    {
        if (count < 1)
        {
            return Result.Failure<(long, long)>(Error.Validation("Count must be at least 1."));
        }

        var startValue = NextValue;
        var newNextValue = startValue + count;

        if (newNextValue <= startValue)
        {
            return Result.Failure<(long, long)>(Error.Validation("Barcode sequence overflow."));
        }

        NextValue = newNextValue;
        return Result.Success((startValue, newNextValue));
    }

    /// <summary>Sıradaki değer ile istemci tahsis modunu günceller.</summary>
    /// <param name="nextValue">Yeni sıradaki barkod değeri; en yüksek tahsis edilmiş değerden büyük olmalıdır.</param>
    /// <param name="clientAllocationRequired">Ürün oluşturmadan önce allocate endpoint'i zorunlu mu.</param>
    /// <param name="maxAllocatedValue">Şu ana kadar tahsis edilmiş en yüksek sayısal barkod.</param>
    public Result UpdateSettings(long nextValue, bool clientAllocationRequired, long maxAllocatedValue)
    {
        if (nextValue < 1)
        {
            return Result.Failure(Error.Validation("Next value must be at least 1."));
        }

        if (nextValue <= maxAllocatedValue)
        {
            return Result.Failure(Error.Conflict(
                $"Next value must be greater than the highest allocated barcode ({maxAllocatedValue})."));
        }

        NextValue = nextValue;
        ClientAllocationRequired = clientAllocationRequired;
        return Result.Success();
    }
}
