namespace Identity.Application.Users.Register;

/// <summary>Yeni kullanıcı kayıt komutu.</summary>
public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string? Name,
    string? TenantName);
