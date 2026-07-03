namespace Identity.Application.Contracts;

/// <summary>/me yanıt modeli.</summary>
public sealed record MeDto(UserDto User, TenantDto Tenant);
