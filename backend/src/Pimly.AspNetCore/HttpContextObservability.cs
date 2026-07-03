using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Pimly.AspNetCore;

/// <summary>HTTP istekleri için trace ve kullanıcı bağlamı yardımcıları.</summary>
public static class HttpContextObservability
{
    /// <summary>Hata yanıtının zaten yapılandırılmış loglandığını işaretleyen HttpContext anahtarı.</summary>
    public const string FailureLoggedItemKey = "Pimly.ApiFailureLogged";

    /// <summary>OpenTelemetry trace kimliğini döndürür; yoksa ASP.NET trace identifier kullanılır.</summary>
    public static string GetTraceId(HttpContext httpContext)
    {
        var activityTraceId = Activity.Current?.TraceId.ToString();
        if (!string.IsNullOrWhiteSpace(activityTraceId))
        {
            return activityTraceId;
        }

        return httpContext.TraceIdentifier;
    }

    /// <summary>JWT <c>sub</c> claim'inden kullanıcı kimliğini döndürür.</summary>
    public static string? GetUserId(ClaimsPrincipal user)
    {
        var subject = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        return string.IsNullOrWhiteSpace(subject) ? null : subject;
    }

    /// <summary>Yanıta <c>X-Trace-Id</c> header'ını ekler.</summary>
    public static void AppendTraceIdHeader(HttpContext httpContext, string traceId)
    {
        if (!httpContext.Response.HasStarted)
        {
            httpContext.Response.Headers["X-Trace-Id"] = traceId;
        }
    }
}
