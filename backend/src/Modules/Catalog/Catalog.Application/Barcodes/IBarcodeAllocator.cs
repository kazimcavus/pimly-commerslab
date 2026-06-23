using SharedKernel;

namespace Catalog.Application.Barcodes;

/// <summary>Sayısal barkod serisinden tahsis yapar.</summary>
public interface IBarcodeAllocator
{
    Task<Result<IReadOnlyList<AllocatedBarcode>>> AllocateAsync(
        int count,
        CancellationToken cancellationToken = default);
}

/// <summary>Tahsis edilen tek bir barkod kaydı.</summary>
/// <param name="AllocationId">Kalıcı tahsis kaydının tanımlayıcısı.</param>
/// <param name="Barcode">Sayısal barkod değeri.</param>
public sealed record AllocatedBarcode(Guid AllocationId, string Barcode);
