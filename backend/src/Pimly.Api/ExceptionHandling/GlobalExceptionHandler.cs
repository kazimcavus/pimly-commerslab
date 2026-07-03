using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Pimly.AspNetCore;
using SharedKernel;

namespace Pimly.Api.ExceptionHandling;

/// <summary>Yakalanmamış istisnaları tutarlı ProblemDetails yanıtlarına dönüştürür.</summary>
internal sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContextObservability.GetTraceId(httpContext);

        logger.LogError(
            exception,
            "Unhandled exception while processing {Method} {Path}. TraceId={TraceId} UserId={UserId}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            traceId,
            HttpContextObservability.GetUserId(httpContext.User) ?? "(anonymous)");

        var (statusCode, title, detail) = MapException(exception);
        var error = new Error(title, detail);

        httpContext.Items[HttpContextObservability.FailureLoggedItemKey] = true;

        var problem = ProblemDetailsFactory.Create(error, traceId);

        if (environment.IsDevelopment())
        {
            problem.Extensions["exception"] = exception.GetType().Name;
            problem.Extensions["stackTrace"] = exception.StackTrace;
        }

        HttpContextObservability.AppendTraceIdHeader(httpContext, traceId);

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }

    private (int StatusCode, string Title, string Detail) MapException(Exception exception) =>
        exception switch
        {
            ArgumentException argument => (
                StatusCodes.Status400BadRequest,
                ErrorCodes.Validation,
                argument.Message),
            KeyNotFoundException notFound => (
                StatusCodes.Status404NotFound,
                ErrorCodes.NotFound,
                notFound.Message),
            InvalidOperationException invalidOperation => (
                StatusCodes.Status409Conflict,
                ErrorCodes.Conflict,
                invalidOperation.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                ErrorCodes.InternalError,
                environment.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred."),
        };
}
