namespace Catalog.Domain;

/// <summary>
/// Bir iş biriminde yapılan değişikliklerin atomik olarak kaydedilmesini sağlayan arabirim.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
