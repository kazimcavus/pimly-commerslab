using SharedKernel;

namespace Identity.Domain.Users;

/// <summary>Kimlik doğrulama için kullanıcı kök varlığı.</summary>
public sealed class User : AggregateRoot<Guid>
{
    private User()
    {
    }

    private User(Guid id, string email, string passwordHash, string name, DateTimeOffset createdAt)
        : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        Name = name;
        CreatedAt = createdAt;
    }

    /// <summary>Gets kullanıcının benzersiz e-posta adresi.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Gets ASP.NET PasswordHasher ile üretilmiş şifre özeti.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>Gets kullanıcının görünen adı.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets kayıt oluşturulma zamanı.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<User> Create(string email, string passwordHash, string? name = null)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Result.Failure<User>(Error.Validation("Email is required."));
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = new User(
            Guid.NewGuid(),
            normalizedEmail,
            passwordHash,
            string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim(),
            DateTimeOffset.UtcNow);

        return Result.Success(user);
    }
}
