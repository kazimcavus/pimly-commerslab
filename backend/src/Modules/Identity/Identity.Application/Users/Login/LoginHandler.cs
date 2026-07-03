using FluentValidation;
using Identity.Application.Auth;
using Identity.Application.Contracts;
using Identity.Application.Validation;
using Identity.Domain;
using Identity.Domain.Tenants;
using SharedKernel;

namespace Identity.Application.Users.Login;

/// <summary>Kullanıcı giriş işlemini yürüten handler.</summary>
public sealed class LoginHandler(
    IValidator<LoginCommand> validator,
    IUserRepository users,
    ITenantMembershipRepository memberships,
    ITenantRepository tenants,
    IPasswordService passwords,
    ITokenService tokens) : ILoginHandler
{
    /// <inheritdoc/>
    public async Task<Result<LoginResult>> ExecuteAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<LoginResult>(validationResult.Error);
        }

        var user = await users.GetByEmailAsync(command.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || !passwords.VerifyPassword(user, command.Password, user.PasswordHash))
        {
            return Result.Failure<LoginResult>(Error.Unauthorized("Invalid credentials."));
        }

        var membership = await memberships.GetPrimaryForUserAsync(user.Id, cancellationToken);
        if (membership is null)
        {
            return Result.Failure<LoginResult>(Error.Unauthorized("User is not assigned to a tenant."));
        }

        var tenant = await tenants.GetByIdAsync(membership.TenantId, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure<LoginResult>(Error.Unauthorized("Tenant not found."));
        }

        var (token, expiresAt) = tokens.GenerateToken(user, tenant);
        return Result.Success(new LoginResult(
            token,
            expiresAt,
            new UserDto(user.Id, user.Email, user.Name),
            new TenantDto(tenant.Id, tenant.Name)));
    }
}
