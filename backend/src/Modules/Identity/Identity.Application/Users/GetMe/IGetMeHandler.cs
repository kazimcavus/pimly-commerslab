using Identity.Application.Contracts;
using SharedKernel;

namespace Identity.Application.Users.GetMe;

/// <summary>GetMeQuery işleyicisi.</summary>
public interface IGetMeHandler
{
    Task<Result<UserDto>> ExecuteAsync(GetMeQuery query, CancellationToken cancellationToken = default);
}
