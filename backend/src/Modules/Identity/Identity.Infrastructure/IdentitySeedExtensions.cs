using Identity.Application.Users.Register;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Identity.Infrastructure;

/// <summary>Geliştirme ortamı için Identity seed yardımcıları.</summary>
public static class IdentitySeedExtensions
{
    /// <summary>
    /// Geliştirme ortamında kayıt akışıyla varsayılan bir kullanıcı + tenant tohumlar.
    /// Kullanıcı zaten varsa hiçbir şey yapmaz. Yalnızca Development'ta çağrılmalıdır.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task SeedIdentityDevUserAsync(
        this IServiceProvider services,
        string email = "owner@acme.test",
        string password = "demo1234",
        string name = "Acme Owner",
        string tenantName = "Acme",
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Identity.Seed");
        var register = scope.ServiceProvider.GetRequiredService<IRegisterUserHandler>();

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var result = await register.ExecuteAsync(
            new RegisterUserCommand(normalizedEmail, password, name, tenantName),
            cancellationToken);

        if (result.IsFailure && result.Error.Code == ErrorCodes.Conflict)
        {
            return;
        }

        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Failed to seed development user: {result.Error.Message}");
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Seeded development user {Email} with tenant {TenantName}.",
                normalizedEmail,
                result.Value.Tenant.Name);
        }
    }
}
