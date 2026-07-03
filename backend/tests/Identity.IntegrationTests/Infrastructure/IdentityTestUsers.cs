using Identity.Application.Users.Register;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel;

namespace Identity.IntegrationTests.Infrastructure;

/// <summary>Integration testleri için kullanıcı seed yardımcıları.</summary>
internal static class IdentityTestUsers
{
    internal static async Task SeedAsync(
        IServiceProvider services,
        string email,
        string password,
        string? name = null,
        string? tenantName = null,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var register = scope.ServiceProvider.GetRequiredService<IRegisterUserHandler>();

        var result = await register.ExecuteAsync(
            new RegisterUserCommand(email, password, name, tenantName ?? name ?? email.Split('@')[0]),
            cancellationToken);

        if (result.IsFailure && result.Error.Code != ErrorCodes.Conflict)
        {
            throw new InvalidOperationException($"Failed to seed test user: {result.Error.Message}");
        }
    }
}
