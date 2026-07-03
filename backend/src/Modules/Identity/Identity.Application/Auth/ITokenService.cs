using Identity.Domain.Tenants;
using Identity.Domain.Users;

namespace Identity.Application.Auth;

/// <summary>JWT üretimi için uygulama katmanı arabirimi.</summary>
public interface ITokenService
{
    (string Token, DateTimeOffset ExpiresAt) GenerateToken(User user, Tenant tenant);
}
