using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Pimly.AspNetCore.Observability;

/// <summary>Health check HTTP yanıt yazarları.</summary>
internal static class HealthCheckResponseWriters
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
    };

    public static Task WriteLivenessResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(
            JsonSerializer.Serialize(new { status = "ok" }, JsonOptions));
    }

    public static Task WriteReadinessResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status == HealthStatus.Healthy ? "ok" : "unhealthy",
            checks = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description,
                    duration_ms = entry.Value.Duration.TotalMilliseconds,
                }),
        };

        if (report.Status != HealthStatus.Healthy)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        }

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
