namespace Inventory.Domain;

/// <summary>
/// Inventory modülünde yapılan değişikliklerin atomik olarak kaydedilmesini sağlayan arabirim.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
