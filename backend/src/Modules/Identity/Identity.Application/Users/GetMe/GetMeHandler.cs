using Identity.Application.Contracts;
using Identity.Domain;
using Identity.Domain.Tenants;
using SharedKernel;

namespace Identity.Application.Users.GetMe;

/// <summary>Aktif kullanıcı ve tenant bilgisini getiren handler.</summary>
public sealed class GetMeHandler(
    IUserRepository users,
    ITenantRepository tenants,
    ITenantMembershipRepository memberships) : IGetMeHandler
{
    /// <inheritdoc/>
    public async Task<Result<MeDto>> ExecuteAsync(
        GetMeQuery query,
        CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(query.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<MeDto>(Error.NotFound("User not found."));
        }

        var membership = await memberships.GetPrimaryForUserAsync(user.Id, cancellationToken);
        if (membership is null || membership.TenantId != query.TenantId)
        {
            return Result.Failure<MeDto>(Error.Unauthorized("Tenant access denied."));
        }

        var tenant = await tenants.GetByIdAsync(query.TenantId, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure<MeDto>(Error.NotFound("Tenant not found."));
        }

        return Result.Success(new MeDto(
            new UserDto(user.Id, user.Email, user.Name),
            new TenantDto(tenant.Id, tenant.Name)));
    }
}
