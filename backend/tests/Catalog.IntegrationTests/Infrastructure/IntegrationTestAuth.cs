using System.Net.Http.Headers;
using System.Net.Http.Json;
using Identity.Application.Users.Register;
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
        return CreateAuthenticatedClient(factory, TestEmail, TestPassword, "Integration Test User", "Integration Test");
    }

    internal static HttpClient CreateAuthenticatedClient(
        WebApplicationFactory<Program> factory,
        string email,
        string password,
        string fullName,
        string tenantName)
    {
        EnsureUserSeededAsync(factory.Services, email, password, fullName, tenantName).GetAwaiter().GetResult();
        var client = factory.CreateClient();
        var token = LoginAsync(client, email, password).GetAwaiter().GetResult();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task EnsureUserSeededAsync(IServiceProvider services)
    {
        await EnsureUserSeededAsync(services, TestEmail, TestPassword, "Integration Test User", "Integration Test");
    }

    private static async Task EnsureUserSeededAsync(
        IServiceProvider services,
        string email,
        string password,
        string fullName,
        string tenantName)
    {
        await using var scope = services.CreateAsyncScope();
        var register = scope.ServiceProvider.GetRequiredService<IRegisterUserHandler>();

        await register.ExecuteAsync(new RegisterUserCommand(
            email,
            password,
            fullName,
            tenantName));
    }

    private static async Task<string> LoginAsync(HttpClient client)
    {
        return await LoginAsync(client, TestEmail, TestPassword);
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/identity/login", new
        {
            email,
            password,
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.Token;
    }

    private sealed record LoginResponse(string Token);
}
