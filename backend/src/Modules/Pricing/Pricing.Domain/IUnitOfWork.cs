namespace Pricing.Domain;

/// <summary>
/// Pricing modülünde yapılan değişikliklerin atomik olarak kaydedilmesini sağlayan arabirim.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
