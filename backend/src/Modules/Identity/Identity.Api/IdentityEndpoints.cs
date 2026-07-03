using System.Security.Claims;
using Identity.Api.Requests;
using Identity.Application.Users.GetMe;
using Identity.Application.Users.Login;
using Identity.Application.Users.Register;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pimly.AspNetCore;
using SharedKernel.Tenancy;

namespace Identity.Api;

/// <summary>Identity modülü REST API uç noktalarını kaydeder.</summary>
public static class IdentityEndpoints
{
    /// <summary>Identity modülü endpoint'lerini uygulama pipeline'ına kaydeder.</summary>
    /// <returns>Kaydedilen route grubu.</returns>
    public static RouteGroupBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/identity").WithTags("Identity");

        group.MapPost("/login", async (LoginRequest request, ILoginHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new LoginCommand(request.Email, request.Password));
            return result.ToHttpResult();
        });

        group.MapPost("/register", async (RegisterUserRequest request, IRegisterUserHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new RegisterUserCommand(
                request.Email,
                request.Password,
                request.Name,
                request.TenantName));

            return result.ToHttpResult(dto => Results.Created("/api/v1/identity/me", dto));
        });

        group.MapGet("/me", async (ClaimsPrincipal principal, IGetMeHandler handler) =>
        {
            if (!TryGetUserId(principal, out var userId) || !TryGetTenantId(principal, out var tenantId))
            {
                return Results.Unauthorized();
            }

            var result = await handler.ExecuteAsync(new GetMeQuery(userId, tenantId));
            return result.ToHttpResult();
        }).RequireAuthorization();

        return group;
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId)
    {
        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        return Guid.TryParse(subject, out userId);
    }

    private static bool TryGetTenantId(ClaimsPrincipal principal, out Guid tenantId)
    {
        var tenant = principal.FindFirstValue(TenantClaimTypes.TenantId);
        return Guid.TryParse(tenant, out tenantId);
    }
}
