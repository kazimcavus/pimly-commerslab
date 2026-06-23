using Catalog.Domain.Barcodes;

namespace Catalog.Application.Contracts;

/// <summary>Barkod domain modelleri ile DTO'lar arasında dönüşüm.</summary>
internal static class BarcodeMappings
{
    public static BarcodeSequenceDto ToDto(this BarcodeSequence sequence) =>
        new(
            sequence.NextValue,
            sequence.ClientAllocationRequired,
            BarcodeAllocation.FormatNumeric(sequence.NextValue));

    public static BarcodeAllocationDto ToDto(this BarcodeAllocation allocation) =>
        new(
            allocation.Id,
            allocation.Barcode,
            allocation.AllocatedAt);
}
