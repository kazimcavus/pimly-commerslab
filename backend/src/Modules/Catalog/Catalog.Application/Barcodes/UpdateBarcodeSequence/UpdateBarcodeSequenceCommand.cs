namespace Catalog.Application.Barcodes.UpdateBarcodeSequence;

/// <summary>Barkod serisi ayarını güncelleme komutu.</summary>
public sealed record UpdateBarcodeSequenceCommand(long NextValue, bool ClientAllocationRequired);
