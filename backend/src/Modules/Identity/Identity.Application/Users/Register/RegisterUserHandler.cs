using FluentValidation;
using Identity.Application.Auth;
using Identity.Application.Contracts;
using Identity.Application.Validation;
using Identity.Domain;
using Identity.Domain.Tenants;
using Identity.Domain.Users;
using SharedKernel;

namespace Identity.Application.Users.Register;

/// <summary>Kayıt sırasında kullanıcı + tenant + üyelik oluşturur.</summary>
public sealed class RegisterUserHandler(
    IValidator<RegisterUserCommand> validator,
    IUserRepository users,
    ITenantRepository tenants,
    ITenantMembershipRepository memberships,
    IPasswordService passwords,
    ITokenService tokens,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : IRegisterUserHandler
{
    /// <inheritdoc/>
    public async Task<Result<LoginResult>> ExecuteAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<LoginResult>(validationResult.Error);
        }

        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        if (await users.GetByEmailAsync(normalizedEmail, cancellationToken) is not null)
        {
            return Result.Failure<LoginResult>(Error.Conflict("Email is already registered."));
        }

        var tenantName = ResolveTenantName(command, normalizedEmail);
        var now = timeProvider.GetUtcNow();

        var tenantResult = Tenant.Create(tenantName, now);
        if (tenantResult.IsFailure)
        {
            return Result.Failure<LoginResult>(tenantResult.Error);
        }

        var draft = User.Create(normalizedEmail, string.Empty, command.Name).Value;
        var passwordHash = passwords.HashPassword(draft, command.Password);
        var userResult = User.Create(normalizedEmail, passwordHash, command.Name);
        if (userResult.IsFailure)
        {
            return Result.Failure<LoginResult>(userResult.Error);
        }

        var membershipResult = TenantMembership.Create(
            tenantResult.Value.Id,
            userResult.Value.Id,
            isPrimary: true,
            now);

        if (membershipResult.IsFailure)
        {
            return Result.Failure<LoginResult>(membershipResult.Error);
        }

        await tenants.AddAsync(tenantResult.Value, cancellationToken);
        await users.AddAsync(userResult.Value, cancellationToken);
        await memberships.AddAsync(membershipResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var (token, expiresAt) = tokens.GenerateToken(userResult.Value, tenantResult.Value);
        return Result.Success(new LoginResult(
            token,
            expiresAt,
            new UserDto(userResult.Value.Id, userResult.Value.Email, userResult.Value.Name),
            new TenantDto(tenantResult.Value.Id, tenantResult.Value.Name)));
    }

    private static string ResolveTenantName(RegisterUserCommand command, string normalizedEmail)
    {
        if (!string.IsNullOrWhiteSpace(command.TenantName))
        {
            return command.TenantName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(command.Name))
        {
            return command.Name.Trim();
        }

        return normalizedEmail.Split('@')[0];
    }
}
