using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace Pimly.AspNetCore;

/// <summary>ProblemDetails yanıtları için ortak fabrika.</summary>
public static class ProblemDetailsFactory
{
    /// <summary>Domain hatasından RFC 7807 ProblemDetails üretir.</summary>
    public static ProblemDetails Create(Error error, string traceId)
    {
        var statusCode = MapStatusCode(error.Code);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = error.Code,
            Detail = error.Message,
        };

        problem.Extensions["trace_id"] = traceId;

        if (error.ValidationErrors is { Count: > 0 })
        {
            problem.Extensions["errors"] = error.ValidationErrors
                .GroupBy(e => e.Field)
                .ToDictionary(
                    g => g.Key,
                    g => (object)g.Select(e => new { code = e.Code, message = e.Message }).ToArray());
        }

        return problem;
    }

    /// <summary>Hata kodunu HTTP durum koduna eşler.</summary>
    public static int MapStatusCode(string errorCode) =>
        errorCode switch
        {
            ErrorCodes.Validation => StatusCodes.Status400BadRequest,
            ErrorCodes.NotFound => StatusCodes.Status404NotFound,
            ErrorCodes.Conflict => StatusCodes.Status409Conflict,
            ErrorCodes.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorCodes.InternalError => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status400BadRequest,
        };
}
