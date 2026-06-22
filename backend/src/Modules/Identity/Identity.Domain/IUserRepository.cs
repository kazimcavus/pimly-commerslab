using Identity.Domain.Users;
using SharedKernel;

namespace Identity.Domain;

/// <summary>Kullanıcı varlıkları için veri erişim arabirimi.</summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
