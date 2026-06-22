using Identity.Domain;
using Identity.Domain.Users;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

/// <summary>Kullanıcı varlıkları için veritabanı erişim katmanı.</summary>
internal sealed class UserRepository(IdentityDbContext db) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        await db.Users.AddAsync(user, cancellationToken);
}
