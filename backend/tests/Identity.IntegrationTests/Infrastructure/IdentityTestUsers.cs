using Identity.Application.Auth;
using Identity.Domain;
using Identity.Domain.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.IntegrationTests.Infrastructure;

/// <summary>Integration testleri için kullanıcı seed yardımcıları.</summary>
internal static class IdentityTestUsers
{
    internal static async Task<User> SeedAsync(
        IServiceProvider services,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var passwords = scope.ServiceProvider.GetRequiredService<IPasswordService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var draft = User.Create(email, string.Empty).Value;
        var passwordHash = passwords.HashPassword(draft, password);
        var user = User.Create(email, passwordHash).Value;

        await users.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return user;
    }
}
