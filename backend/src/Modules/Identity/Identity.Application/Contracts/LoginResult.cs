using Identity.Application.Contracts;

namespace Identity.Application.Contracts;

/// <summary>Başarılı giriş yanıtı.</summary>
public sealed record LoginResult(string Token, DateTimeOffset ExpiresAt, UserDto User);
