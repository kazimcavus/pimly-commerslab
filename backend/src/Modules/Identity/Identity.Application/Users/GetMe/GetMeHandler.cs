using Identity.Application.Contracts;
using Identity.Domain;
using SharedKernel;

namespace Identity.Application.Users.GetMe;

/// <summary>Aktif kullanıcı bilgisini getiren handler.</summary>
public sealed class GetMeHandler(IUserRepository users) : IGetMeHandler
{
    /// <inheritdoc/>
    public async Task<Result<UserDto>> ExecuteAsync(
        GetMeQuery query,
        CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(query.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<UserDto>(Error.NotFound("User not found."));
        }

        return Result.Success(new UserDto(user.Id, user.Email, user.Name));
    }
}
