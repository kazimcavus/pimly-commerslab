namespace Catalog.Application.Barcodes.ListBarcodeAllocations;

/// <summary>Barkod tahsis kayıtlarını listeleme sorgusu.</summary>
public sealed record ListBarcodeAllocationsQuery(int Page = 0, int PageSize = 0);
