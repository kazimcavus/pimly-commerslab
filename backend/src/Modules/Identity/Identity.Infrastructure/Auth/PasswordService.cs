using Identity.Application.Auth;
using Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace Identity.Infrastructure.Auth;

/// <summary>ASP.NET PasswordHasher tabanlı şifre servisi.</summary>
internal sealed class PasswordService(IPasswordHasher<User> passwordHasher) : IPasswordService
{
    public string HashPassword(User user, string password) =>
        passwordHasher.HashPassword(user, password);

    public bool VerifyPassword(User user, string password, string passwordHash) =>
        passwordHasher.VerifyHashedPassword(user, passwordHash, password)
            == PasswordVerificationResult.Success;
}
