using System.Net.Http.Headers;
using System.Net.Http.Json;
using Identity.Application.Auth;
using Identity.Domain;
using Identity.Domain.Users;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.IntegrationTests.Infrastructure;

/// <summary>Integration testleri için JWT kimlik doğrulama yardımcıları.</summary>
internal static class IntegrationTestAuth
{
    private const string TestEmail = "integration-test@example.com";
    private const string TestPassword = "integration-test-password";

    internal static HttpClient CreateAuthenticatedClient(WebApplicationFactory<Program> factory)
    {
        EnsureUserSeededAsync(factory.Services).GetAwaiter().GetResult();
        var client = factory.CreateClient();
        var token = LoginAsync(client).GetAwaiter().GetResult();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task EnsureUserSeededAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        if (await users.GetByEmailAsync(TestEmail) is not null)
        {
            return;
        }

        var passwords = scope.ServiceProvider.GetRequiredService<IPasswordService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var draft = User.Create(TestEmail, string.Empty).Value;
        var passwordHash = passwords.HashPassword(draft, TestPassword);
        var user = User.Create(TestEmail, passwordHash).Value;

        await users.AddAsync(user);
        await unitOfWork.SaveChangesAsync();
    }

    private static async Task<string> LoginAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/identity/login", new
        {
            email = TestEmail,
            password = TestPassword,
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.Token;
    }

    private sealed record LoginResponse(string Token);
}
