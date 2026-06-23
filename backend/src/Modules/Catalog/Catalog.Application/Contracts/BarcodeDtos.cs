namespace Catalog.Application.Contracts;

/// <summary>Barkod serisi ayar DTO'su.</summary>
public sealed record BarcodeSequenceDto(
    long NextValue,
    bool ClientAllocationRequired,
    string NextPreview);

/// <summary>Tahsis edilen barkod listesi sonucu.</summary>
public sealed record AllocateBarcodesResult(IReadOnlyList<string> Barcodes);

/// <summary>Barkod tahsis kaydı DTO'su.</summary>
public sealed record BarcodeAllocationDto(
    Guid Id,
    string Barcode,
    DateTimeOffset AllocatedAt);
