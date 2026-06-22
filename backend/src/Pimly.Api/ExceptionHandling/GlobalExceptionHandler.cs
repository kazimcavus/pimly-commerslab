using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
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
        logger.LogError(
            exception,
            "Unhandled exception while processing {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        var (statusCode, title, detail) = MapException(exception);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
        };

        if (environment.IsDevelopment())
        {
            problem.Extensions["exception"] = exception.GetType().Name;
            problem.Extensions["stackTrace"] = exception.StackTrace;
        }

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
