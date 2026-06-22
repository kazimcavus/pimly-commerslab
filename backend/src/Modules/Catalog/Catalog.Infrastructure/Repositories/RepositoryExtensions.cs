using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Repositories;

/// <summary>Repository güncelleme yardımcıları.</summary>
internal static class RepositoryExtensions
{
    /// <summary>
    /// Zaten izlenen aggregate köklerinde owned collection değişikliklerini bozmadan kaydeder.
    /// </summary>
    internal static void UpdateIfDetached<T>(DbContext db, T entity)
        where T : class
    {
        if (db.Entry(entity).State == EntityState.Detached)
        {
            db.Update(entity);
        }
    }
}
