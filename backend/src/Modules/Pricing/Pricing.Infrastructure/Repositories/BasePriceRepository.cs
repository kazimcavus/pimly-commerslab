using Microsoft.EntityFrameworkCore;
using Pricing.Domain.BasePrices;
using Pricing.Infrastructure.Persistence;

namespace Pricing.Infrastructure.Repositories;

/// <summary>BasePrice aggregate için veritabanı erişim katmanı.</summary>
internal sealed class BasePriceRepository(PricingDbContext db) : IBasePriceRepository
{
    public async Task<BasePrice?> GetByItemAsync(Guid productItemId, CancellationToken cancellationToken = default) =>
        await db.BasePrices
            .FirstOrDefaultAsync(p => p.ProductItemId == productItemId, cancellationToken);

    public async Task AddAsync(BasePrice basePrice, CancellationToken cancellationToken = default) =>
        await db.BasePrices.AddAsync(basePrice, cancellationToken);

    public void Update(BasePrice basePrice) => db.BasePrices.Update(basePrice);

    public void Remove(BasePrice basePrice) => db.BasePrices.Remove(basePrice);
}
