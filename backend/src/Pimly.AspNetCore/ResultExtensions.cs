using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace Pimly.AspNetCore;

/// <summary>Domain Result türlerini HTTP yanıtlarına dönüştüren uzantı metodları.</summary>
public static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        return result.Error.ToProblemResult();
    }

    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult>? onSuccess = null)
    {
        if (result.IsSuccess)
        {
            return onSuccess?.Invoke(result.Value) ?? Results.Ok(result.Value);
        }

        return result.Error.ToProblemResult();
    }

    public static IResult ToCreatedResult<T>(this Result<T> result, Func<T, string> locationFactory)
    {
        if (result.IsSuccess)
        {
            return Results.Created(locationFactory(result.Value), result.Value);
        }

        return result.Error.ToProblemResult();
    }

    private static IResult ToProblemResult(this Error error)
    {
        var statusCode = error.Code switch
        {
            ErrorCodes.Validation => StatusCodes.Status400BadRequest,
            ErrorCodes.NotFound => StatusCodes.Status404NotFound,
            ErrorCodes.Conflict => StatusCodes.Status409Conflict,
            ErrorCodes.Unauthorized => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status400BadRequest,
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = error.Code,
            Detail = error.Message,
        };

        if (error.ValidationErrors is { Count: > 0 })
        {
            problem.Extensions["errors"] = error.ValidationErrors
                .GroupBy(e => e.Field)
                .ToDictionary(
                    g => g.Key,
                    g => (object)g.Select(e => new { code = e.Code, message = e.Message }).ToArray());
        }

        return Results.Problem(problem);
    }
}
