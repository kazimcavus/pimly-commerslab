using Catalog.Application.Barcodes;
using Catalog.Domain.Barcodes;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Tenancy;

namespace Catalog.Infrastructure.Barcodes;

/// <summary>PostgreSQL üzerinde atomik barkod tahsisi gerçekleştirir.</summary>
internal sealed class BarcodeAllocator(
    CatalogDbContext db,
    ITenantContext tenantContext) : IBarcodeAllocator
{
    public async Task<Result<IReadOnlyList<AllocatedBarcode>>> AllocateAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        if (count < 1)
        {
            return Result.Failure<IReadOnlyList<AllocatedBarcode>>(
                Error.Validation("Count must be at least 1."));
        }

        var tenantId = tenantContext.TenantId;

        var startRow = (await db.Database
            .SqlQuery<SequenceReserveRow>(
                $"""
                 UPDATE catalog.barcode_sequence
                 SET next_value = next_value + {count}
                 WHERE tenant_id = {tenantId} AND id = {BarcodeSequence.SingletonId}
                 RETURNING (next_value - {count})::bigint AS "StartValue"
                 """)
            .ToListAsync(cancellationToken))
            .SingleOrDefault();

        if (startRow is null)
        {
            return Result.Failure<IReadOnlyList<AllocatedBarcode>>(
                Error.NotFound("Barcode sequence is not configured."));
        }

        var startValue = startRow.StartValue;
        var allocations = new List<BarcodeAllocation>(count);
        var results = new List<AllocatedBarcode>(count);

        for (var index = 0; index < count; index++)
        {
            var barcode = BarcodeAllocation.FormatNumeric(startValue + index);
            var createResult = BarcodeAllocation.Create(barcode);
            if (createResult.IsFailure)
            {
                return Result.Failure<IReadOnlyList<AllocatedBarcode>>(createResult.Error);
            }

            allocations.Add(createResult.Value);
            results.Add(new AllocatedBarcode(createResult.Value.Id, barcode));
        }

        await db.BarcodeAllocations.AddRangeAsync(allocations, cancellationToken);

        var trackedSequence = await db.BarcodeSequences
            .FirstOrDefaultAsync(
                sequence => sequence.Id == BarcodeSequence.SingletonId,
                cancellationToken);

        if (trackedSequence is not null)
        {
            db.Entry(trackedSequence).Reload();
        }

        return Result.Success<IReadOnlyList<AllocatedBarcode>>(results);
    }

    private sealed record SequenceReserveRow(long StartValue);
}
