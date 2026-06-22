using FluentValidation;
using Identity.Application.Auth;
using Identity.Application.Contracts;
using Identity.Application.Validation;
using Identity.Domain;
using SharedKernel;

namespace Identity.Application.Users.Login;

/// <summary>Kullanıcı giriş işlemini yürüten handler.</summary>
public sealed class LoginHandler(
    IValidator<LoginCommand> validator,
    IUserRepository users,
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

        var (token, expiresAt) = tokens.GenerateToken(user);
        return Result.Success(new LoginResult(
            token,
            expiresAt,
            new UserDto(user.Id, user.Email, user.Name)));
    }
}
