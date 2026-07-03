using Microsoft.AspNetCore.Http;
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

        return new LoggingProblemResult(result.Error);
    }

    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult>? onSuccess = null)
    {
        if (result.IsSuccess)
        {
            return onSuccess?.Invoke(result.Value) ?? Results.Ok(result.Value);
        }

        return new LoggingProblemResult(result.Error);
    }

    public static IResult ToCreatedResult<T>(this Result<T> result, Func<T, string> locationFactory)
    {
        if (result.IsSuccess)
        {
            return Results.Created(locationFactory(result.Value), result.Value);
        }

        return new LoggingProblemResult(result.Error);
    }
}
