using Identity.Application.Contracts;
using SharedKernel;

namespace Identity.Application.Users.Register;

/// <summary>RegisterUserCommand işleyicisi.</summary>
public interface IRegisterUserHandler
{
    Task<Result<LoginResult>> ExecuteAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken = default);
}
