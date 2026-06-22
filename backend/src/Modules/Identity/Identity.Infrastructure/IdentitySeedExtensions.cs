using Identity.Application.Auth;
using Identity.Domain;
using Identity.Domain.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure;

/// <summary>Geliştirme ortamı için Identity seed yardımcıları.</summary>
public static class IdentitySeedExtensions
{
    /// <summary>
    /// Geliştirme ortamında giriş yapılabilmesi için varsayılan bir kullanıcı tohumlar.
    /// Kullanıcı zaten varsa hiçbir şey yapmaz. Yalnızca Development'ta çağrılmalıdır.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task SeedIdentityDevUserAsync(
        this IServiceProvider services,
        string email = "owner@acme.test",
        string password = "demo1234",
        string name = "Acme Owner",
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Identity.Seed");
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var passwords = scope.ServiceProvider.GetRequiredService<IPasswordService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (await users.GetByEmailAsync(normalizedEmail, cancellationToken) is not null)
        {
            return;
        }

        var draft = User.Create(normalizedEmail, string.Empty, name).Value;
        var passwordHash = passwords.HashPassword(draft, password);
        var user = User.Create(normalizedEmail, passwordHash, name).Value;

        await users.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Seeded development user {Email}.", normalizedEmail);
        }
    }
}
