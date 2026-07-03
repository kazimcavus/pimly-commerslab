namespace Identity.Api.Requests;

/// <summary>Kullanıcı kayıt isteği.</summary>
public sealed record RegisterUserRequest(
    string Email,
    string Password,
    string? Name,
    string? TenantName);
