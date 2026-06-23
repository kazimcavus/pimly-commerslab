using Catalog.Application.SkuGenerator;
using Catalog.Domain.SkuGenerator;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Catalog.Infrastructure.SkuGenerator;

/// <summary>PostgreSQL üzerinde atomik SKU counter rezervasyonu gerçekleştirir.</summary>
internal sealed class SkuCounterAllocator(CatalogDbContext db) : ISkuCounterAllocator
{
    public async Task<Result<long>> ReserveAsync(int count, CancellationToken cancellationToken = default)
    {
        if (count < 1)
        {
            return Result.Failure<long>(Error.Validation("Count must be at least 1."));
        }

        var startRow = await db.Database
            .SqlQuery<CounterReserveRow>(
                $"""
                 UPDATE catalog.sku_generator_config
                 SET counter_next_value = counter_next_value + {count}
                 WHERE id = {SkuGeneratorConfig.SingletonId}
                 RETURNING (counter_next_value - {count})::bigint AS "StartValue"
                 """)
            .SingleOrDefaultAsync(cancellationToken);

        if (startRow is null)
        {
            return Result.Failure<long>(Error.NotFound("SKU generator is not configured."));
        }

        var trackedConfig = await db.SkuGeneratorConfigs.FindAsync(
            [SkuGeneratorConfig.SingletonId],
            cancellationToken);

        if (trackedConfig is not null)
        {
            db.Entry(trackedConfig).Reload();
        }

        return Result.Success(startRow.StartValue);
    }

    private sealed record CounterReserveRow(long StartValue);
}
