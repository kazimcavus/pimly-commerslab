namespace Catalog.Api.Requests;

/// <summary>Barkod serisi ayarını güncelleme isteği.</summary>
public sealed record UpdateBarcodeSequenceRequest(
    long NextValue,
    bool ClientAllocationRequired);

/// <summary>Barkod tahsisi isteği.</summary>
public sealed record AllocateBarcodesRequest(int Count = 1);
