using Catalog.Domain;
using Catalog.Domain.Barcodes;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Catalog.Infrastructure.Repositories;

/// <summary>Barkod tahsis kayıtları için veritabanı erişim katmanı.</summary>
internal sealed class BarcodeAllocationRepository(CatalogDbContext db) : IBarcodeAllocationRepository
{
    public async Task<bool> ExistsAsync(string barcode, CancellationToken cancellationToken = default) =>
        await db.BarcodeAllocations.AnyAsync(a => a.Barcode == barcode, cancellationToken);

    public async Task<long> MaxNumericBarcodeAsync(CancellationToken cancellationToken = default)
    {
        var barcodes = await db.BarcodeAllocations
            .Select(a => a.Barcode)
            .ToListAsync(cancellationToken);

        return barcodes
            .Where(BarcodeAllocation.IsNumericBarcode)
            .Select(static barcode => long.Parse(
                barcode,
                System.Globalization.CultureInfo.InvariantCulture))
            .DefaultIfEmpty(0L)
            .Max();
    }

    public async Task AddRangeAsync(
        IEnumerable<BarcodeAllocation> allocations,
        CancellationToken cancellationToken = default) =>
        await db.BarcodeAllocations.AddRangeAsync(allocations, cancellationToken);

    public async Task<PagedResult<BarcodeAllocation>> ListAsync(
        Pagination pagination,
        CancellationToken cancellationToken = default) =>
        await db.BarcodeAllocations
            .OrderByDescending(a => a.AllocatedAt)
            .ThenByDescending(a => a.Barcode)
            .ToPagedResultAsync(pagination, cancellationToken);
}
