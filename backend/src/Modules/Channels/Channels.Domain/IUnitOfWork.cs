namespace Channels.Domain;

/// <summary>
/// Channels modülünde yapılan değişikliklerin atomik olarak kaydedilmesini sağlayan arabirim.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
