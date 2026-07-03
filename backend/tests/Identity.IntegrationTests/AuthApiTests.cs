using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Identity.IntegrationTests.Infrastructure;

namespace Identity.IntegrationTests;

/// <summary>Identity auth API E2E testleri.</summary>
[Collection(IdentityIntegrationCollection.Name)]
public class AuthApiTests(IdentityPostgresFixture fixture)
{
    [SkippableFact]
    public async Task Register_CreatesUserWithOwnTenant()
    {
        IdentityPostgresFixture.SkipIfUnavailable(fixture);
        var client = fixture.Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/identity/register", new
        {
            email = "new-shop@example.com",
            password = "secret1234",
            name = "Shop Owner",
            tenant_name = "My Shop",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body.Should().NotBeNull();
        body!.Token.Should().NotBeNullOrWhiteSpace();
        body.User.Email.Should().Be("new-shop@example.com");
        body.Tenant.Name.Should().Be("My Shop");
    }

    [SkippableFact]
    public async Task Register_WithExistingEmail_ReturnsConflict()
    {
        IdentityPostgresFixture.SkipIfUnavailable(fixture);
        var client = fixture.Factory.CreateClient();
        await IdentityTestUsers.SeedAsync(fixture.Factory.Services, "dup@example.com", "secret1234");

        var response = await client.PostAsJsonAsync("/api/v1/identity/register", new
        {
            email = "dup@example.com",
            password = "secret1234",
            name = "Duplicate",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [SkippableFact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        IdentityPostgresFixture.SkipIfUnavailable(fixture);
        var client = fixture.Factory.CreateClient();
        await IdentityTestUsers.SeedAsync(fixture.Factory.Services, "auth@example.com", "secret123");

        var response = await client.PostAsJsonAsync("/api/v1/identity/login", new
        {
            email = "auth@example.com",
            password = "secret123",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body.Should().NotBeNull();
        body!.Token.Should().NotBeNullOrWhiteSpace();
        body.User.Email.Should().Be("auth@example.com");
    }

    [SkippableFact]
    public async Task Login_WithInvalidPassword_Returns401()
    {
        IdentityPostgresFixture.SkipIfUnavailable(fixture);
        var client = fixture.Factory.CreateClient();
        await IdentityTestUsers.SeedAsync(fixture.Factory.Services, "bad@example.com", "secret123");

        var response = await client.PostAsJsonAsync("/api/v1/identity/login", new
        {
            email = "bad@example.com",
            password = "wrong",
        });

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        problem!.Title.Should().Be("unauthorized");
    }

    [SkippableFact]
    public async Task Me_WithBearerToken_ReturnsCurrentUser()
    {
        IdentityPostgresFixture.SkipIfUnavailable(fixture);
        var client = fixture.Factory.CreateClient();
        await IdentityTestUsers.SeedAsync(fixture.Factory.Services, "me@example.com", "secret123");

        var login = await client.PostAsJsonAsync("/api/v1/identity/login", new
        {
            email = "me@example.com",
            password = "secret123",
        });
        var loginBody = await login.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.Token);

        var response = await client.GetAsync("/api/v1/identity/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MeResponse>();
        body!.User.Email.Should().Be("me@example.com");
    }

    [SkippableFact]
    public async Task Me_WithoutToken_Returns401()
    {
        IdentityPostgresFixture.SkipIfUnavailable(fixture);
        var client = fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/v1/identity/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [SkippableFact]
    public async Task CatalogCategories_WithoutToken_Returns401()
    {
        IdentityPostgresFixture.SkipIfUnavailable(fixture);
        var client = fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/v1/catalog/categories");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record LoginResponse(
        string Token,
        DateTimeOffset ExpiresAt,
        UserResponse User,
        TenantResponse Tenant);

    private sealed record MeResponse(
        UserResponse User,
        TenantResponse Tenant);

    private sealed record UserResponse(
        Guid Id,
        string Email,
        string Name);

    private sealed record TenantResponse(
        Guid Id,
        string Name);

    private sealed record ProblemDetailsResponse(
        [property: JsonPropertyName("title")] string? Title,
        int? Status,
        string? Detail);
}
