using Identity.Domain.Users;

namespace Identity.Application.Auth;

/// <summary>Şifre hash ve doğrulama işlemleri için uygulama katmanı arabirimi.</summary>
public interface IPasswordService
{
    string HashPassword(User user, string password);

    bool VerifyPassword(User user, string password, string passwordHash);
}
