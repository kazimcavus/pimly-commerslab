using Identity.Application.Contracts;
using SharedKernel;

namespace Identity.Application.Users.Login;

/// <summary>LoginCommand işleyicisi.</summary>
public interface ILoginHandler
{
    Task<Result<LoginResult>> ExecuteAsync(LoginCommand command, CancellationToken cancellationToken = default);
}
