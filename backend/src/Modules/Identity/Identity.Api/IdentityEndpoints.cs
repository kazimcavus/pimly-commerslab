using System.Security.Claims;
using Identity.Api.Requests;
using Identity.Application.Users.GetMe;
using Identity.Application.Users.Login;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pimly.AspNetCore;

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

        group.MapGet("/me", async (ClaimsPrincipal principal, IGetMeHandler handler) =>
        {
            if (!TryGetUserId(principal, out var userId))
            {
                return Results.Unauthorized();
            }

            var result = await handler.ExecuteAsync(new GetMeQuery(userId));
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
}
