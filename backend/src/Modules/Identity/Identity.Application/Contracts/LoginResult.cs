namespace Identity.Application.Contracts;

/// <summary>Başarılı giriş yanıt modeli.</summary>
public sealed record LoginResult(
    string Token,
    DateTimeOffset ExpiresAt,
    UserDto User,
    TenantDto Tenant);
