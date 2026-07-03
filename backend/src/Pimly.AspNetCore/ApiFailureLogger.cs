using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Pimly.AspNetCore;

/// <summary>API hata yanıtları için yapılandırılmış log kaydı.</summary>
internal static class ApiFailureLogger
{
    internal const string LoggerCategory = "Pimly.Api.RequestFailure";

    internal static void Log(HttpContext httpContext, Error error, int statusCode)
    {
        var logger = httpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(LoggerCategory);

        var level = statusCode >= StatusCodes.Status500InternalServerError
            ? LogLevel.Error
            : LogLevel.Warning;

        if (!logger.IsEnabled(level))
        {
            return;
        }

        logger.Log(
            level,
            "Request {Method} {Path} failed with {StatusCode} {ErrorCode}: {ErrorMessage}. UserId={UserId} ValidationFields={ValidationFields}",
            httpContext.Request.Method,
            httpContext.Request.Path.Value,
            statusCode,
            error.Code,
            error.Message,
            HttpContextObservability.GetUserId(httpContext.User) ?? "(anonymous)",
            FormatValidationFields(error.ValidationErrors));
    }

    private static string FormatValidationFields(IReadOnlyList<ValidationError>? errors) =>
        errors is null or { Count: 0 }
            ? string.Empty
            : string.Join(", ", errors.Select(e => $"{e.Field}:{e.Code}"));
}
